using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiaPhuWaterDelivery.Api.Controllers;

[ApiController]
[Route("api/containers")]
[Authorize(Roles = "Admin,Sales")]
public sealed class ContainersController(AppDbContext db) : ControllerBase
{
    [HttpGet("transactions")]
    public async Task<PagedResult<ContainerTransactionDto>> Transactions(
        int page = 1, int pageSize = 10, int? customerId = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.ContainerTransactions.AsNoTracking().AsQueryable();
        if (customerId.HasValue)
            query = query.Where(x => x.CustomerId == customerId.Value);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ContainerTransactionDto(
                x.Id, x.CustomerId, x.Customer.CustomerName,
                x.ProductId, x.Product.ProductName, x.OrderId, x.Order.OrderCode,
                x.DeliveredQuantity, x.ReturnedQuantity, x.BalanceBefore, x.BalanceAfter,
                x.CreatedAt, x.CreatedByUser.FullName, x.Note))
            .ToArrayAsync(cancellationToken);
        return PagedResult<ContainerTransactionDto>.Create(items, count, page, pageSize);
    }
}
