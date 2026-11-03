using GiaPhuWaterDelivery.Api.DTOs;
using GiaPhuWaterDelivery.Api.Models;
using GiaPhuWaterDelivery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GiaPhuWaterDelivery.Api.Controllers;

[ApiController]
[Route("api/deliveries")]
[Authorize(Roles = "Delivery")]
public sealed class DeliveriesController(DeliveryService service, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet("my-orders")]
    public Task<PagedResult<DeliveryDto>> MyOrders(
        int page = 1, int pageSize = 10, string? search = null,
        DeliveryStatus? status = null, CancellationToken cancellationToken = default) =>
        service.MyOrdersAsync(page, pageSize, search, status, cancellationToken);

    [HttpGet("{id:int}")]
    public Task<DeliveryDto> Get(int id, CancellationToken cancellationToken) =>
        service.GetAsync(id, cancellationToken);

    [HttpPost("{id:int}/start")]
    public Task<DeliveryDto> Start(int id, CancellationToken cancellationToken) =>
        service.StartAsync(id, cancellationToken);

    [HttpPost("{id:int}/complete")]
    public Task<DeliveryDto> Complete(
        int id, CompleteDeliveryRequest request, CancellationToken cancellationToken) =>
        service.CompleteAsync(id, request, cancellationToken);

    [HttpPost("{id:int}/fail")]
    public Task<DeliveryDto> Fail(
        int id, FailDeliveryRequest request, CancellationToken cancellationToken) =>
        service.FailAsync(id, request, cancellationToken);

    [HttpPost("{id:int}/proof")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5_500_000)]
    public async Task<DeliveryDto> UploadProof(
        int id, [FromForm] UploadDeliveryProofRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await service.GetAsync(id, cancellationToken);
        if (existing.Status is not (DeliveryStatus.Delivering or DeliveryStatus.Completed))
            throw new Helpers.BusinessException(StatusCodes.Status409Conflict,
                "Chỉ đơn đang giao hoặc đã giao mới có thể lưu bằng chứng.", "invalid_delivery_status");
        const long maxBytes = 5 * 1024 * 1024;
        var allowedTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };
        if (request.Image.Length == 0 || request.Image.Length > maxBytes)
            throw new Helpers.BusinessException(StatusCodes.Status400BadRequest,
                "Ảnh bằng chứng phải có dung lượng từ 1 byte đến 5 MB.", "invalid_proof_size");
        if (!allowedTypes.TryGetValue(request.Image.ContentType, out var extension))
            throw new Helpers.BusinessException(StatusCodes.Status400BadRequest,
                "Chỉ chấp nhận ảnh JPEG, PNG hoặc WebP.", "invalid_proof_type");

        var relativeDirectory = Path.Combine("uploads", "delivery-proofs");
        var physicalDirectory = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), relativeDirectory);
        Directory.CreateDirectory(physicalDirectory);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(physicalDirectory, fileName);
        await using (var input = request.Image.OpenReadStream())
        {
            var header = new byte[12];
            var bytesRead = await input.ReadAsync(header, cancellationToken);
            if (!HasValidSignature(request.Image.ContentType, header, bytesRead))
                throw new Helpers.BusinessException(StatusCodes.Status400BadRequest,
                    "Nội dung tệp không đúng định dạng ảnh đã khai báo.", "invalid_proof_content");
            input.Position = 0;
            await using var output = System.IO.File.Create(physicalPath);
            await input.CopyToAsync(output, cancellationToken);
        }

        try
        {
            var result = await service.SaveProofAsync(id, request.RecipientName,
                $"/uploads/delivery-proofs/{fileName}", cancellationToken);
            if (!string.IsNullOrWhiteSpace(existing.ProofImageUrl))
            {
                var oldName = Path.GetFileName(existing.ProofImageUrl);
                if (!string.IsNullOrWhiteSpace(oldName) && oldName != fileName)
                    System.IO.File.Delete(Path.Combine(physicalDirectory, oldName));
            }
            return result;
        }
        catch
        {
            System.IO.File.Delete(physicalPath);
            throw;
        }
    }

    private static bool HasValidSignature(string contentType, byte[] bytes, int count) =>
        contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase)
            ? count >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF
            : contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase)
                ? count >= 8 && bytes.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })
                : contentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase)
                    && count >= 12
                    && bytes.Take(4).SequenceEqual("RIFF"u8.ToArray())
                    && bytes.Skip(8).Take(4).SequenceEqual("WEBP"u8.ToArray());
}
