using System.Data;
using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.DTOs;
using GiaPhuWaterDelivery.Api.Helpers;
using GiaPhuWaterDelivery.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiaPhuWaterDelivery.Api.Controllers;

[ApiController]
[Route("api/containers")]
[Authorize(Roles = "Admin,Sales")]
public sealed class ContainersController(AppDbContext db, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("transactions")]
    public async Task<PagedResult<ContainerTransactionDto>> Transactions(
        int page = 1, int pageSize = 10, int? customerId = null,
        ContainerTransactionType? transactionType = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.ContainerTransactions.AsNoTracking().AsQueryable();
        if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
        if (transactionType.HasValue) query = query.Where(x => x.TransactionType == transactionType.Value);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ContainerTransactionDto(
                x.Id, x.CustomerId, x.Customer.CustomerName,
                x.ProductId, x.Product.ProductName, x.OrderId,
                x.Order == null ? null : x.Order.OrderCode,
                x.TransactionType, x.QuantityChange,
                x.DeliveredQuantity, x.ReturnedQuantity, x.BalanceBefore, x.BalanceAfter,
                x.CreatedAt, x.CreatedByUser.FullName, x.Note))
            .ToArrayAsync(cancellationToken);
        return PagedResult<ContainerTransactionDto>.Create(items, count, page, pageSize);
    }

    [HttpPost("returns")]
    public Task<ActionResult<ContainerTransactionDto>> Return(
        ContainerReturnEntryRequest request, CancellationToken cancellationToken) =>
        ChangeBalance(request.CustomerId, request.ProductId, -request.Quantity,
            ContainerTransactionType.Return,
            string.IsNullOrWhiteSpace(request.Note) ? "Khách trả vỏ ngoài đơn hàng" : request.Note.Trim(),
            cancellationToken);

    [Authorize(Roles = "Admin")]
    [HttpPost("adjustments")]
    public Task<ActionResult<ContainerTransactionDto>> Adjust(
        ContainerAdjustmentRequest request, CancellationToken cancellationToken)
    {
        if (request.QuantityChange == 0)
            throw new BusinessException(StatusCodes.Status400BadRequest,
                "Số lượng điều chỉnh phải khác 0.", "invalid_container_adjustment");
        return ChangeBalance(request.CustomerId, request.ProductId, request.QuantityChange,
            ContainerTransactionType.Adjustment, request.Reason.Trim(), cancellationToken);
    }

    private async Task<ActionResult<ContainerTransactionDto>> ChangeBalance(
        int customerId, int productId, int quantityChange,
        ContainerTransactionType type, string note, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var customer = await db.Customers.SingleOrDefaultAsync(
            x => x.Id == customerId && x.IsActive, cancellationToken)
            ?? throw new BusinessException(StatusCodes.Status404NotFound,
                "Không tìm thấy khách hàng.", "customer_not_found");
        var product = await db.Products.SingleOrDefaultAsync(
            x => x.Id == productId && x.IsActive && x.RequiresContainer, cancellationToken)
            ?? throw new BusinessException(StatusCodes.Status400BadRequest,
                "Sản phẩm không tồn tại hoặc không quản lý vỏ.", "invalid_container_product");
        var before = customer.ContainerBalance;
        var after = before + quantityChange;
        if (after < 0)
            throw new BusinessException(StatusCodes.Status409Conflict,
                "Số vỏ trả vượt quá số vỏ khách đang giữ.", "container_balance_insufficient");

        customer.ContainerBalance = after;
        var entity = new ContainerTransaction
        {
            CustomerId = customerId,
            ProductId = productId,
            TransactionType = type,
            QuantityChange = quantityChange,
            ReturnedQuantity = quantityChange < 0 ? -quantityChange : 0,
            DeliveredQuantity = quantityChange > 0 ? quantityChange : 0,
            BalanceBefore = before,
            BalanceAfter = after,
            CreatedByUserId = currentUser.Id,
            CreatedAt = DateTime.UtcNow,
            Note = note
        };
        db.ContainerTransactions.Add(entity);
        AuditTrail.Add(db, currentUser, type == ContainerTransactionType.Return
            ? "ContainerReturned" : "ContainerAdjusted", "Customer", customerId,
            new { containerBalance = before },
            new { containerBalance = after, productId, quantityChange, note },
            HttpContext.Connection.RemoteIpAddress?.ToString());
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Ok(new ContainerTransactionDto(
            entity.Id, customer.Id, customer.CustomerName, product.Id, product.ProductName,
            null, null, entity.TransactionType, entity.QuantityChange,
            entity.DeliveredQuantity, entity.ReturnedQuantity, before, after,
            entity.CreatedAt, currentUser.DisplayName, note));
    }
}
