using GiaPhuWaterDelivery.Api.DTOs;
using GiaPhuWaterDelivery.Api.Models;
using GiaPhuWaterDelivery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GiaPhuWaterDelivery.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public sealed class OrdersController(OrderService service, InvoiceService invoiceService) : ControllerBase
{
    [Authorize(Roles = "Admin,Sales,Warehouse,Delivery")]
    [HttpGet]
    public Task<PagedResult<OrderDto>> GetAll(
        int page = 1, int pageSize = 10, string? search = null,
        OrderStatus? status = null, int? customerId = null,
        DateTime? fromDate = null, DateTime? toDate = null,
        CancellationToken cancellationToken = default) =>
        service.ListAsync(
            page, pageSize, search, status, customerId, fromDate, toDate, cancellationToken);

    [Authorize(Roles = "Admin,Sales,Warehouse,Delivery")]
    [HttpGet("{id:int}")]
    public Task<OrderDto> Get(int id, CancellationToken cancellationToken) =>
        service.GetAsync(id, cancellationToken);

    [Authorize(Roles = "Sales")]
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(
        CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [Authorize(Roles = "Sales")]
    [HttpPut("{id:int}")]
    public Task<OrderDto> Update(
        int id, UpdateOrderRequest request, CancellationToken cancellationToken) =>
        service.UpdateAsync(id, request, cancellationToken);

    [Authorize(Roles = "Sales")]
    [HttpPost("{id:int}/confirm")]
    public Task<OrderDto> Confirm(int id, CancellationToken cancellationToken) =>
        service.ConfirmAsync(id, cancellationToken);

    [Authorize(Roles = "Warehouse")]
    [HttpPost("{id:int}/prepare")]
    public Task<OrderDto> Prepare(int id, CancellationToken cancellationToken) =>
        service.PrepareAsync(id, cancellationToken);

    [Authorize(Roles = "Warehouse")]
    [HttpPost("{id:int}/ready")]
    public Task<OrderDto> Ready(int id, CancellationToken cancellationToken) =>
        service.ReadyAsync(id, cancellationToken);

    [Authorize(Roles = "Sales")]
    [HttpPost("{id:int}/cancel")]
    public Task<OrderDto> Cancel(int id, CancellationToken cancellationToken) =>
        service.CancelAsync(id, cancellationToken);

    [Authorize(Roles = "Admin,Sales")]
    [HttpPost("{id:int}/assign-delivery")]
    public Task<OrderDto> AssignDelivery(
        int id, AssignDeliveryRequest request, CancellationToken cancellationToken) =>
        service.AssignDeliveryAsync(id, request.DeliveryUserId, cancellationToken);

    [Authorize(Roles = "Admin,Sales")]
    [HttpPost("{id:int}/payments")]
    public Task<OrderDto> RecordPayment(
        int id, RecordPaymentRequest request, CancellationToken cancellationToken) =>
        service.RecordPaymentAsync(id, request, cancellationToken);

    [Authorize(Roles = "Admin,Sales")]
    [HttpGet("{id:int}/invoice")]
    public async Task<IActionResult> Invoice(int id, CancellationToken cancellationToken)
    {
        var result = await invoiceService.GenerateAsync(id, cancellationToken);
        return File(result.Content, "application/pdf", result.FileName);
    }
}
