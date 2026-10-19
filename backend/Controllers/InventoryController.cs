using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.DTOs;
using GiaPhuWaterDelivery.Api.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiaPhuWaterDelivery.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize(Roles = "Admin,Warehouse")]
public sealed class InventoryController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<ProductDto>> GetInventory(
        int page = 1, int pageSize = 10, string? search = null, bool? lowStock = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Products.AsNoTracking().Include(x => x.Category).Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.ProductCode.Contains(term) || x.ProductName.Contains(term));
        }
        if (lowStock.HasValue)
            query = lowStock.Value
                ? query.Where(x => x.StockQuantity < x.MinimumStock)
                : query.Where(x => x.StockQuantity >= x.MinimumStock);
        var count = await query.CountAsync(cancellationToken);
        var entities = await query.OrderBy(x => x.ProductName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);
        return PagedResult<ProductDto>.Create(
            entities.Select(x => x.ToDto()).ToArray(), count, page, pageSize);
    }

    [HttpGet("transactions")]
    public async Task<PagedResult<InventoryTransactionDto>> GetTransactions(
        int page = 1, int pageSize = 10, string? search = null,
        int? productId = null, Models.InventoryTransactionType? transactionType = null,
        DateTime? from = null, DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.InventoryTransactions.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.Product.ProductCode.Contains(term) ||
                x.Product.ProductName.Contains(term) ||
                (x.Order != null && x.Order.OrderCode.Contains(term)));
        }
        if (productId.HasValue)
            query = query.Where(x => x.ProductId == productId);
        if (transactionType.HasValue)
            query = query.Where(x => x.TransactionType == transactionType);
        if (from.HasValue)
            query = query.Where(x => x.CreatedAt >= from.Value.ToUniversalTime());
        if (to.HasValue)
            query = query.Where(x => x.CreatedAt <= to.Value.ToUniversalTime());

        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new InventoryTransactionDto(
                x.Id, x.ProductId, x.Product.ProductCode, x.Product.ProductName,
                x.OrderId, x.Order == null ? null : x.Order.OrderCode,
                x.TransactionType, x.Quantity, x.QuantityBefore, x.QuantityAfter,
                x.CreatedByUserId, x.CreatedByUser.FullName, x.CreatedAt, x.Note))
            .ToListAsync(cancellationToken);
        return PagedResult<InventoryTransactionDto>.Create(items, count, page, pageSize);
    }
}
