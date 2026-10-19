using System.Data;
using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.DTOs;
using GiaPhuWaterDelivery.Api.Helpers;
using GiaPhuWaterDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GiaPhuWaterDelivery.Api.Services;

public sealed class DeliveryService(AppDbContext db, ICurrentUser currentUser)
{
    private IQueryable<Delivery> FullQuery(bool tracking = false)
    {
        var query = db.Deliveries
            .Include(x => x.DeliveryUser)
            .Include(x => x.Order).ThenInclude(x => x.Customer)
            .Include(x => x.Order).ThenInclude(x => x.CreatedByUser)
            .Include(x => x.Order).ThenInclude(x => x.DeliveryUser)
            .Include(x => x.Order).ThenInclude(x => x.Details).ThenInclude(x => x.Product)
            .Include(x => x.Order).ThenInclude(x => x.Payments).ThenInclude(x => x.RecordedByUser)
            .AsSplitQuery();
        return tracking ? query : query.AsNoTracking();
    }

    public async Task<PagedResult<DeliveryDto>> MyOrdersAsync(
        int page, int pageSize, string? search, DeliveryStatus? status,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = FullQuery().Where(x => x.DeliveryUserId == currentUser.Id);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.Order.OrderCode.Contains(term) ||
                x.Order.Customer.CustomerName.Contains(term) ||
                x.Order.DeliveryAddress.Contains(term));
        }
        if (status.HasValue)
            query = query.Where(x => x.Status == status);

        var count = await query.CountAsync(cancellationToken);
        var entities = await query.OrderByDescending(x => x.AssignedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PagedResult<DeliveryDto>.Create(
            entities.Select(x => x.ToDto(true)).ToArray(), count, page, pageSize);
    }

    public async Task<DeliveryDto> GetAsync(int id, CancellationToken cancellationToken)
    {
        var delivery = await FullQuery().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw NotFound();
        EnsureOwner(delivery);
        return delivery.ToDto(true);
    }

    public async Task<DeliveryDto> StartAsync(int id, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var delivery = await FullQuery(true).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw NotFound();
        EnsureOwner(delivery);
        if (delivery.Status != DeliveryStatus.Assigned)
            throw InvalidStatus("Chỉ đơn đã phân công mới có thể bắt đầu giao.");
        OrderStateMachine.Ensure(delivery.Order.OrderStatus, OrderStatus.Delivering);

        delivery.Status = DeliveryStatus.Delivering;
        delivery.StartedAt = DateTime.UtcNow;
        delivery.Order.OrderStatus = OrderStatus.Delivering;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetTrackedResultAsync(id, cancellationToken);
    }

    public async Task<DeliveryDto> CompleteAsync(
        int id, CompleteDeliveryRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var delivery = await FullQuery(true).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw NotFound();
        EnsureOwner(delivery);
        if (delivery.Status != DeliveryStatus.Delivering)
            throw InvalidStatus("Chỉ đơn đang giao mới có thể hoàn tất.");
        OrderStateMachine.Ensure(delivery.Order.OrderStatus, OrderStatus.Delivered);

        var duplicateReturn = request.ContainerReturns
            .GroupBy(x => x.OrderDetailId).Any(x => x.Count() > 1);
        if (duplicateReturn)
            throw new BusinessException(StatusCodes.Status400BadRequest,
                "Mỗi dòng sản phẩm chỉ được khai báo số vỏ thu hồi một lần.",
                "duplicate_container_return");
        var returns = request.ContainerReturns.ToDictionary(x => x.OrderDetailId, x => x.Quantity);
        var validDetailIds = delivery.Order.Details.Where(x => x.Product.RequiresContainer)
            .Select(x => x.Id).ToHashSet();
        if (returns.Keys.Any(idValue => !validDetailIds.Contains(idValue)))
            throw new BusinessException(StatusCodes.Status400BadRequest,
                "Dữ liệu vỏ bình thu hồi không hợp lệ.", "invalid_container_return");

        var depositAmount = 0m;
        var customer = delivery.Order.Customer;
        foreach (var detail in delivery.Order.Details.Where(x => x.Product.RequiresContainer))
        {
            var returned = returns.GetValueOrDefault(detail.Id);
            if (returned < 0 || returned > detail.Quantity)
                throw new BusinessException(StatusCodes.Status400BadRequest,
                    $"Số vỏ thu hồi của {detail.Product.ProductName} phải từ 0 đến {detail.Quantity}.",
                    "invalid_container_return_quantity");
            var before = customer.ContainerBalance;
            customer.ContainerBalance += detail.Quantity - returned;
            detail.ReturnedContainerQuantity = returned;
            detail.ContainerDepositAmount = (detail.Quantity - returned) * detail.Product.ContainerDeposit;
            depositAmount += detail.ContainerDepositAmount;
            db.ContainerTransactions.Add(new ContainerTransaction
            {
                CustomerId = customer.Id,
                ProductId = detail.ProductId,
                OrderId = delivery.OrderId,
                DeliveredQuantity = detail.Quantity,
                ReturnedQuantity = returned,
                BalanceBefore = before,
                BalanceAfter = customer.ContainerBalance,
                CreatedByUserId = currentUser.Id,
                CreatedAt = DateTime.UtcNow,
                Note = $"Giao và thu hồi vỏ theo đơn {delivery.Order.OrderCode}"
            });
        }

        delivery.Order.ProductAmount = delivery.Order.Details.Sum(x => x.TotalPrice);
        delivery.Order.DepositAmount = depositAmount;
        delivery.Order.TotalAmount = delivery.Order.ProductAmount + depositAmount;

        delivery.Status = DeliveryStatus.Completed;
        delivery.CompletedAt = DateTime.UtcNow;
        delivery.Note = Clean(request.Note) ?? delivery.Note;
        delivery.Order.OrderStatus = OrderStatus.Delivered;
        if (request.MarkAsPaid)
        {
            var remaining = delivery.Order.TotalAmount - delivery.Order.PaidAmount;
            if (remaining > 0)
            {
                db.Payments.Add(new Payment
                {
                    OrderId = delivery.OrderId,
                    Amount = remaining,
                    PaymentMethod = delivery.Order.PaymentMethod,
                    PaidAt = DateTime.UtcNow,
                    RecordedByUserId = currentUser.Id,
                    Note = "Thanh toán khi hoàn tất giao hàng"
                });
                delivery.Order.PaidAmount += remaining;
            }
        }
        delivery.Order.PaymentStatus = OrderService.GetPaymentStatus(
            delivery.Order.PaidAmount, delivery.Order.TotalAmount);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetTrackedResultAsync(id, cancellationToken);
    }

    public async Task<DeliveryDto> FailAsync(
        int id, FailDeliveryRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FailureReason))
            throw new BusinessException(StatusCodes.Status400BadRequest,
                "Lý do giao thất bại là bắt buộc.", "failure_reason_required");

        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var delivery = await FullQuery(true).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw NotFound();
        EnsureOwner(delivery);
        if (delivery.Status != DeliveryStatus.Delivering)
            throw InvalidStatus("Chỉ đơn đang giao mới có thể báo giao thất bại.");
        OrderStateMachine.Ensure(delivery.Order.OrderStatus, OrderStatus.DeliveryFailed);

        foreach (var detail in delivery.Order.Details)
        {
            var before = detail.Product.StockQuantity;
            detail.Product.StockQuantity += detail.Quantity;
            db.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = detail.ProductId,
                OrderId = delivery.OrderId,
                TransactionType = InventoryTransactionType.StockIn,
                Quantity = detail.Quantity,
                QuantityBefore = before,
                QuantityAfter = detail.Product.StockQuantity,
                CreatedByUserId = currentUser.Id,
                CreatedAt = DateTime.UtcNow,
                Note = $"Hoàn kho do giao thất bại đơn {delivery.Order.OrderCode}"
            });
        }
        delivery.Status = DeliveryStatus.Failed;
        delivery.CompletedAt = DateTime.UtcNow;
        delivery.FailureReason = request.FailureReason.Trim();
        delivery.Note = Clean(request.Note);
        delivery.Order.OrderStatus = OrderStatus.DeliveryFailed;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetTrackedResultAsync(id, cancellationToken);
    }

    private async Task<DeliveryDto> GetTrackedResultAsync(int id, CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();
        var value = await FullQuery().SingleAsync(x => x.Id == id, cancellationToken);
        return value.ToDto(true);
    }

    private void EnsureOwner(Delivery delivery)
    {
        if (delivery.DeliveryUserId != currentUser.Id)
            throw new BusinessException(StatusCodes.Status403Forbidden,
                "Đơn giao hàng không được phân công cho bạn.", "forbidden_delivery");
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static BusinessException NotFound() =>
        new(StatusCodes.Status404NotFound, "Không tìm thấy giao hàng.", "delivery_not_found");
    private static BusinessException InvalidStatus(string message) =>
        new(StatusCodes.Status409Conflict, message, "invalid_delivery_status");
}
