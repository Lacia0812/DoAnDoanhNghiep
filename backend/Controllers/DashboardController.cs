using GiaPhuWaterDelivery.Api.DTOs;
using GiaPhuWaterDelivery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GiaPhuWaterDelivery.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(DashboardService service) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpGet("summary")]
    public Task<DashboardSummaryDto> Summary(CancellationToken cancellationToken) =>
        service.SummaryAsync(cancellationToken);

    [Authorize(Roles = "Admin")]
    [HttpGet("revenue")]
    public Task<IReadOnlyCollection<RevenuePointDto>> Revenue(
        int months = 12, CancellationToken cancellationToken = default) =>
        service.RevenueAsync(months, cancellationToken);

    [Authorize(Roles = "Admin")]
    [HttpGet("order-status")]
    public Task<IReadOnlyCollection<OrderStatusPointDto>> OrderStatus(
        CancellationToken cancellationToken) =>
        service.OrderStatusAsync(cancellationToken);

    [Authorize(Roles = "Admin")]
    [HttpGet("top-products")]
    public Task<IReadOnlyCollection<TopProductDto>> TopProducts(
        int take = 5, CancellationToken cancellationToken = default) =>
        service.TopProductsAsync(take, cancellationToken);

    [Authorize(Roles = "Admin,Warehouse")]
    [HttpGet("restock-suggestions")]
    public Task<IReadOnlyCollection<RestockSuggestionDto>> RestockSuggestions(
        CancellationToken cancellationToken) =>
        service.RestockSuggestionsAsync(cancellationToken);
}
