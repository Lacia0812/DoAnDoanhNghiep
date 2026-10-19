using GiaPhuWaterDelivery.Api.DTOs;
using GiaPhuWaterDelivery.Api.Models;
using GiaPhuWaterDelivery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GiaPhuWaterDelivery.Api.Controllers;

[ApiController]
[Route("api/deliveries")]
[Authorize(Roles = "Delivery")]
public sealed class DeliveriesController(DeliveryService service) : ControllerBase
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
}
