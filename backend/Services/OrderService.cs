using System.Data;
using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.DTOs;
using GiaPhuWaterDelivery.Api.Helpers;
using GiaPhuWaterDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GiaPhuWaterDelivery.Api.Services;

public sealed class OrderService(AppDbContext db, ICurrentUser currentUser)
{
    private IQueryable<Order> FullQuery(bool tracking = false)
    {
        var query = db.Orders
            .Include(x => x.Customer)
            .Include(x => x.CreatedByUser)
            .Include(x => x.DeliveryUser)
            .Include(x => x.Details).ThenInclude(x => x.Product)
            .Include(x => x.Payments).ThenInclude(x => x.RecordedByUser)
            .Include(x => x.Delivery).ThenInclude(x => x!.DeliveryUser)
            .AsSplitQuery();
        return tracking ? query : query.AsNoTracking();
    }

    public async Task<PagedResult<OrderDto>> ListAsync(
        int page, int pageSize, string? search, OrderStatus? status, int? customerId,
        CancellationToken cancellationToken)
        => await ListAsync(
            page, pageSize, search, status, customerId, null, null, cancellationToken);

    public async Task<PagedResult<OrderDto>> ListAsync(
        int page, int pageSize, string? search, OrderStatus? status, int? customerId,
        DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken)
    {
        (page, pageSize) = NormalizePaging(page, pageSize);
        IQueryable<Order> query = FullQuery();

        if (currentUser.Role == UserRole.Warehouse.ToString())
        {
            var statuses = new[]
            {
                OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.ReadyForDelivery
            };
            query = query.Where(x => statuses.Contains(x.OrderStatus));
        }
        else if (currentUser.Role == UserRole.Delivery.ToString())
        {
            query = query.Where(x => x.DeliveryUserId == currentUser.Id);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.OrderCode.Contains(term) ||
                x.Customer.CustomerName.Contains(term) ||
                x.Customer.Phone.Contains(term));
        }
        if (status.HasValue)
            query = query.Where(x => x.OrderStatus == status);
        if (customerId.HasValue)
            query = query.Where(x => x.CustomerId == customerId);
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
            throw new BusinessException(StatusCodes.Status400BadRequest,
                "Ngày bắt đầu không được sau ngày kết thúc.", "invalid_date_range");
        if (fromDate.HasValue)
        {
            var fromUtc = VietnamTime.ToUtc(fromDate.Value.Date);
            query = query.Where(x => x.OrderDate >= fromUtc);
        }
        if (toDate.HasValue)
        {
            var toExclusiveUtc = VietnamTime.ToUtc(toDate.Value.Date.AddDays(1));
            query = query.Where(x => x.OrderDate < toExclusiveUtc);
        }

        var total = await query.CountAsync(cancellationToken);
        var entities = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PagedResult<OrderDto>.Create(
            entities.Select(x => x.ToDto()).ToArray(), total, page, pageSize);
    }

    public async Task<OrderDto> GetAsync(int id, CancellationToken cancellationToken)
    {
        var order = await FullQuery().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw NotFound();
        EnsureCanView(order);
        return order.ToDto();
    }

    public async Task<OrderDto> CreateAsync(
        CreateOrderRequest request, CancellationToken cancellationToken)
    {
        ValidateItems(request);
        var paymentMethod = request.PaymentMethod
            ?? throw PaymentMethodRequired();
        ValidateExpectedDeliveryDate(request.ExpectedDeliveryDate, VietnamTime.Now.Date);
        var customer = await db.Customers.SingleOrDefaultAsync(
            x => x.Id == request.CustomerId && x.IsActive, cancellationToken)
            ?? throw new BusinessException(StatusCodes.Status400BadRequest,
                "Khách hàng không tồn tại hoặc đã ngừng hoạt động.", "invalid_customer");
        var products = await LoadProductsAsync(request.Items, cancellationToken);

        var order = new Order
        {
            OrderCode = await GenerateOrderCodeAsync(cancellationToken),
            CustomerId = customer.Id,
            CreatedByUserId = currentUser.Id,
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate.HasValue
                ? VietnamTime.ToUtc(request.ExpectedDeliveryDate.Value.Date)
                : null,
            DeliveryAddress = request.DeliveryAddress.Trim(),
            PaymentMethod = paymentMethod,
            PaymentStatus = PaymentStatus.Unpaid,
            OrderStatus = OrderStatus.New,
            Note = Clean(request.Note),
            CreatedAt = DateTime.UtcNow
        };
        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];
            order.Details.Add(new OrderDetail
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.Price,
                TotalPrice = product.Price * item.Quantity,
                ReturnedContainerQuantity = 0,
                ContainerDepositAmount = 0
            });
        }
        order.ProductAmount = order.Details.Sum(x => x.TotalPrice);
        order.DepositAmount = 0;
        order.PaidAmount = 0;
        order.TotalAmount = order.ProductAmount;
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        return await GetWithoutPermissionAsync(order.Id, cancellationToken);
    }

    public async Task<OrderDto> UpdateAsync(
        int id, UpdateOrderRequest request, CancellationToken cancellationToken)
    {
        ValidateItems(request);
        var paymentMethod = request.PaymentMethod
            ?? throw PaymentMethodRequired();
        var order = await db.Orders.Include(x => x.Details)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw NotFound();
        EnsureStatus(order, OrderStatus.New);
        var orderDateUtc = DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc);
        var localOrderDate = TimeZoneInfo.ConvertTimeFromUtc(orderDateUtc, VietnamTime.Zone).Date;
        ValidateExpectedDeliveryDate(request.ExpectedDeliveryDate, localOrderDate);

        var customer = await db.Customers.SingleOrDefaultAsync(
            x => x.Id == request.CustomerId && x.IsActive, cancellationToken)
            ?? throw new BusinessException(StatusCodes.Status400BadRequest,
                "Khách hàng không tồn tại hoặc đã ngừng hoạt động.", "invalid_customer");
        var products = await LoadProductsAsync(request.Items, cancellationToken);

        db.OrderDetails.RemoveRange(order.Details);
        order.Details.Clear();
        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];
            order.Details.Add(new OrderDetail
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.Price,
                TotalPrice = product.Price * item.Quantity,
                ReturnedContainerQuantity = 0,
                ContainerDepositAmount = 0
            });
        }
        order.CustomerId = customer.Id;
        order.ExpectedDeliveryDate = request.ExpectedDeliveryDate.HasValue
            ? VietnamTime.ToUtc(request.ExpectedDeliveryDate.Value.Date)
            : null;
        order.DeliveryAddress = request.DeliveryAddress.Trim();
        order.PaymentMethod = paymentMethod;
        order.Note = Clean(request.Note);
        order.ProductAmount = order.Details.Sum(x => x.TotalPrice);
        order.DepositAmount = 0;
        order.TotalAmount = order.ProductAmount;
        await db.SaveChangesAsync(cancellationToken);
        return await GetWithoutPermissionAsync(order.Id, cancellationToken);
    }

    public async Task<OrderDto> ConfirmAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Orders.Include(x => x.Details).ThenInclude(x => x.Product)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw NotFound();
        OrderStateMachine.Ensure(order.OrderStatus, OrderStatus.Confirmed);
        foreach (var detail in order.Details)
        {
            if (!detail.Product.IsActive || detail.Product.StockQuantity < detail.Quantity)
                throw InsufficientStock(detail.Product.ProductName);
        }
        order.OrderStatus = OrderStatus.Confirmed;
        await db.SaveChangesAsync(cancellationToken);
        return await GetWithoutPermissionAsync(id, cancellationToken);
    }

    public async Task<OrderDto> PrepareAsync(int id, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var order = await db.Orders.Include(x => x.Details).ThenInclude(x => x.Product)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw NotFound();
        OrderStateMachine.Ensure(order.OrderStatus, OrderStatus.Preparing);

        foreach (var detail in order.Details)
        {
            if (!detail.Product.IsActive || detail.Product.StockQuantity < detail.Quantity)
                throw InsufficientStock(detail.Product.ProductName);
        }
        foreach (var detail in order.Details)
        {
            var before = detail.Product.StockQuantity;
            detail.Product.StockQuantity -= detail.Quantity;
            db.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = detail.ProductId,
                OrderId = order.Id,
                TransactionType = InventoryTransactionType.StockOut,
                Quantity = detail.Quantity,
                QuantityBefore = before,
                QuantityAfter = detail.Product.StockQuantity,
                CreatedByUserId = currentUser.Id,
                CreatedAt = DateTime.UtcNow,
                Note = $"Xuất kho cho đơn {order.OrderCode}"
            });
        }
        order.OrderStatus = OrderStatus.Preparing;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetWithoutPermissionAsync(id, cancellationToken);
    }

    public async Task<OrderDto> ReadyAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Orders.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw NotFound();
        OrderStateMachine.Ensure(order.OrderStatus, OrderStatus.ReadyForDelivery);
        order.OrderStatus = OrderStatus.ReadyForDelivery;
        await db.SaveChangesAsync(cancellationToken);
        return await GetWithoutPermissionAsync(id, cancellationToken);
    }

    public async Task<OrderDto> CancelAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Orders.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw NotFound();
        OrderStateMachine.Ensure(order.OrderStatus, OrderStatus.Cancelled);
        order.OrderStatus = OrderStatus.Cancelled;
        await db.SaveChangesAsync(cancellationToken);
        return await GetWithoutPermissionAsync(id, cancellationToken);
    }

    public async Task<OrderDto> AssignDeliveryAsync(
        int id, int deliveryUserId, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var order = await db.Orders.Include(x => x.Delivery)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw NotFound();
        if (order.OrderStatus != OrderStatus.ReadyForDelivery)
            throw InvalidStatus("Chỉ được phân công khi đơn đã sẵn sàng giao.");

        var user = await db.Users.SingleOrDefaultAsync(
            x => x.Id == deliveryUserId && x.IsActive && x.Role == UserRole.Delivery,
            cancellationToken)
            ?? throw new BusinessException(StatusCodes.Status400BadRequest,
                "Nhân viên giao hàng không hợp lệ.", "invalid_delivery_user");

        if (order.Delivery is null)
        {
            order.Delivery = new Delivery
            {
                DeliveryUserId = user.Id,
                AssignedAt = DateTime.UtcNow,
                Status = DeliveryStatus.Assigned
            };
        }
        else
        {
            if (order.Delivery.Status != DeliveryStatus.Assigned || order.Delivery.StartedAt.HasValue)
                throw InvalidStatus("Không thể đổi nhân viên sau khi đã bắt đầu giao.");
            order.Delivery.DeliveryUserId = user.Id;
            order.Delivery.AssignedAt = DateTime.UtcNow;
        }
        order.DeliveryUserId = user.Id;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetWithoutPermissionAsync(id, cancellationToken);
    }

    public async Task<OrderDto> RecordPaymentAsync(
        int id, RecordPaymentRequest request, CancellationToken cancellationToken)
    {
        var paymentMethod = request.PaymentMethod
            ?? throw PaymentMethodRequired();
        await using var transaction = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);
        var order = await db.Orders.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw NotFound();
        if (order.OrderStatus != OrderStatus.Delivered)
            throw InvalidStatus("Chỉ được ghi nhận thanh toán cho đơn đã giao thành công.");
        var remaining = order.TotalAmount - order.PaidAmount;
        if (request.Amount <= 0 || request.Amount > remaining)
            throw new BusinessException(StatusCodes.Status400BadRequest,
                $"Số tiền thanh toán phải lớn hơn 0 và không vượt quá {remaining:N0} đồng.",
                "invalid_payment_amount");

        db.Payments.Add(new Payment
        {
            OrderId = order.Id,
            Amount = request.Amount,
            PaymentMethod = paymentMethod,
            PaidAt = DateTime.UtcNow,
            RecordedByUserId = currentUser.Id,
            Note = Clean(request.Note)
        });
        order.PaidAmount += request.Amount;
        order.PaymentStatus = GetPaymentStatus(order.PaidAmount, order.TotalAmount);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetWithoutPermissionAsync(id, cancellationToken);
    }

    internal static PaymentStatus GetPaymentStatus(decimal paidAmount, decimal totalAmount) =>
        paidAmount <= 0 ? PaymentStatus.Unpaid
        : paidAmount >= totalAmount ? PaymentStatus.Paid
        : PaymentStatus.Partial;

    private async Task<OrderDto> GetWithoutPermissionAsync(int id, CancellationToken cancellationToken)
    {
        var value = await FullQuery().SingleAsync(x => x.Id == id, cancellationToken);
        return value.ToDto();
    }

    private async Task<Dictionary<int, Product>> LoadProductsAsync(
        IReadOnlyCollection<OrderItemRequest> items, CancellationToken cancellationToken)
    {
        var ids = items.Select(x => x.ProductId).Distinct().ToArray();
        var products = await db.Products.Where(x => ids.Contains(x.Id) && x.IsActive)
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        if (products.Count != ids.Length)
            throw new BusinessException(StatusCodes.Status400BadRequest,
                "Một hoặc nhiều sản phẩm không hợp lệ.", "invalid_product");
        return products;
    }

    private static void ValidateItems(CreateOrderRequest request)
    {
        if (request.Items.Count == 0)
            throw new BusinessException(StatusCodes.Status400BadRequest,
                "Đơn hàng phải có ít nhất một sản phẩm.", "empty_order");
        if (request.Items.Any(x => x.ProductId <= 0 || x.Quantity <= 0))
            throw new BusinessException(StatusCodes.Status400BadRequest,
                "Sản phẩm và số lượng đặt không hợp lệ.", "invalid_order_item");
        if (request.Items.GroupBy(x => x.ProductId).Any(x => x.Count() > 1))
            throw new BusinessException(StatusCodes.Status400BadRequest,
                "Không được chọn trùng sản phẩm trong một đơn.", "duplicate_order_item");
        if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
            throw new BusinessException(StatusCodes.Status400BadRequest,
                "Địa chỉ giao hàng là bắt buộc.", "delivery_address_required");
    }

    private static void ValidateExpectedDeliveryDate(
        DateTime? expectedDeliveryDate, DateTime minimumLocalDate)
    {
        if (expectedDeliveryDate.HasValue &&
            expectedDeliveryDate.Value.Date < minimumLocalDate.Date)
            throw new BusinessException(StatusCodes.Status400BadRequest,
                "Ngày giao dự kiến không được trước ngày đặt hàng.",
                "invalid_expected_delivery_date");
    }

    private void EnsureCanView(Order order)
    {
        if (currentUser.Role == UserRole.Delivery.ToString() &&
            order.DeliveryUserId != currentUser.Id)
            throw new BusinessException(StatusCodes.Status403Forbidden,
                "Bạn không được xem đơn hàng này.", "forbidden_order");
        if (currentUser.Role == UserRole.Warehouse.ToString() &&
            order.OrderStatus is not (OrderStatus.Confirmed or OrderStatus.Preparing or OrderStatus.ReadyForDelivery))
            throw new BusinessException(StatusCodes.Status403Forbidden,
                "Đơn hàng không thuộc luồng xử lý kho.", "forbidden_order");
    }

    private static void EnsureStatus(Order order, OrderStatus expected)
    {
        if (order.OrderStatus != expected)
            throw InvalidStatus($"Chỉ đơn ở trạng thái {expected} mới được thực hiện thao tác này.");
    }

    private async Task<string> GenerateOrderCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var code = $"DH{DateTime.UtcNow:yyyyMMddHHmmssfff}{Random.Shared.Next(10, 99)}";
            if (!await db.Orders.AnyAsync(x => x.OrderCode == code, cancellationToken))
                return code;
        }
        return $"DH{Guid.NewGuid():N}"[..30].ToUpperInvariant();
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize) =>
        (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
    private static BusinessException NotFound() =>
        new(StatusCodes.Status404NotFound, "Không tìm thấy đơn hàng.", "order_not_found");
    private static BusinessException InvalidStatus(string message) =>
        new(StatusCodes.Status409Conflict, message, "invalid_order_status");
    private static BusinessException InsufficientStock(string product) =>
        new(StatusCodes.Status409Conflict, $"Sản phẩm \"{product}\" không đủ tồn kho.", "insufficient_stock");
    private static BusinessException PaymentMethodRequired() =>
        new(StatusCodes.Status400BadRequest,
            "Phương thức thanh toán là bắt buộc.", "payment_method_required");
}
