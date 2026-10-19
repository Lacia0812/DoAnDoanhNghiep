using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.DTOs;
using GiaPhuWaterDelivery.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiaPhuWaterDelivery.Api.Controllers;

[ApiController]
[Route("api/debts")]
[Authorize(Roles = "Admin,Sales")]
public sealed class DebtsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<DebtDto>> GetAll(
        int page = 1, int pageSize = 10, string? search = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Orders.AsNoTracking()
            .Where(x => x.OrderStatus == OrderStatus.Delivered && x.PaidAmount < x.TotalAmount);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.OrderCode.Contains(term)
                || x.Customer.CustomerName.Contains(term)
                || x.Customer.Phone.Contains(term));
        }
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Delivery!.CompletedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new DebtDto(
                x.Id, x.OrderCode, x.CustomerId, x.Customer.CustomerCode,
                x.Customer.CustomerName, x.Customer.Phone,
                x.Delivery != null && x.Delivery.CompletedAt.HasValue
                    ? x.Delivery.CompletedAt.Value : x.OrderDate,
                x.TotalAmount, x.PaidAmount, x.TotalAmount - x.PaidAmount,
                x.PaymentStatus))
            .ToArrayAsync(cancellationToken);
        return PagedResult<DebtDto>.Create(items, count, page, pageSize);
    }

    [HttpGet("summary")]
    public async Task<DebtSummaryDto> Summary(CancellationToken cancellationToken)
    {
        var query = db.Orders.AsNoTracking()
            .Where(x => x.OrderStatus == OrderStatus.Delivered && x.PaidAmount < x.TotalAmount);
        return new DebtSummaryDto(
            await query.SumAsync(x => x.TotalAmount - x.PaidAmount, cancellationToken),
            await query.CountAsync(cancellationToken),
            await query.Select(x => x.CustomerId).Distinct().CountAsync(cancellationToken));
    }
}
