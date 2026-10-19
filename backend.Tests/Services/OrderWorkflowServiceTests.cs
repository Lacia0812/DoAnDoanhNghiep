using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.DTOs;
using GiaPhuWaterDelivery.Api.Helpers;
using GiaPhuWaterDelivery.Api.Models;
using GiaPhuWaterDelivery.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GiaPhuWaterDelivery.Api.Tests.Services;

public sealed class OrderWorkflowServiceTests
{
    [Fact]
    public async Task Create_order_uses_current_database_price_and_calculates_totals()
    {
        await using var db = CreateContext();
        AddPrincipals(db);
        db.Products.Add(Product(1, price: 12_500m, stock: 10));
        await db.SaveChangesAsync();

        var result = await new OrderService(db, CurrentUser(1, UserRole.Sales))
            .CreateAsync(
                OrderRequest(new OrderItemRequest(1, 3)),
                CancellationToken.None);

        var detail = Assert.Single(result.Details);
        Assert.Equal(12_500m, detail.UnitPrice);
        Assert.Equal(37_500m, detail.TotalPrice);
        Assert.Equal(37_500m, result.TotalAmount);
        Assert.Equal(OrderStatus.New, result.OrderStatus);
    }

    [Fact]
    public async Task Create_order_rejects_duplicate_products()
    {
        await using var db = CreateContext();
        AddPrincipals(db);
        db.Products.Add(Product(1, price: 10m, stock: 10));
        await db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => new OrderService(db, CurrentUser(1, UserRole.Sales))
                .CreateAsync(
                    OrderRequest(
                        new OrderItemRequest(1, 1),
                        new OrderItemRequest(1, 2)),
                    CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Equal("duplicate_order_item", exception.Code);
        Assert.Empty(db.Orders);
    }

    [Fact]
    public async Task Confirm_checks_stock_without_reserving_it()
    {
        await using var db = CreateContext();
        AddPrincipals(db);
        db.Products.Add(Product(1, price: 10m, stock: 5));
        await db.SaveChangesAsync();
        var service = new OrderService(db, CurrentUser(1, UserRole.Sales));
        var created = await service.CreateAsync(
            OrderRequest(new OrderItemRequest(1, 3)),
            CancellationToken.None);

        var result = await service.ConfirmAsync(created.Id, CancellationToken.None);

        Assert.Equal(OrderStatus.Confirmed, result.OrderStatus);
        Assert.Equal(5, (await db.Products.FindAsync(1))!.StockQuantity);
        Assert.Empty(db.InventoryTransactions);
    }

    [Fact]
    public async Task Prepare_rechecks_stock_and_never_partially_deducts()
    {
        await using var db = CreateContext();
        AddPrincipals(db);
        db.Products.AddRange(
            Product(1, price: 10m, stock: 5),
            Product(2, price: 20m, stock: 5));
        await db.SaveChangesAsync();
        var service = new OrderService(db, CurrentUser(1, UserRole.Warehouse));
        var created = await service.CreateAsync(
            OrderRequest(
                new OrderItemRequest(1, 3),
                new OrderItemRequest(2, 3)),
            CancellationToken.None);
        await service.ConfirmAsync(created.Id, CancellationToken.None);
        (await db.Products.FindAsync(2))!.StockQuantity = 2;
        await db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.PrepareAsync(created.Id, CancellationToken.None));

        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);
        Assert.Equal("insufficient_stock", exception.Code);
        Assert.Equal(5, (await db.Products.FindAsync(1))!.StockQuantity);
        Assert.Equal(2, (await db.Products.FindAsync(2))!.StockQuantity);
        Assert.Empty(db.InventoryTransactions);
        Assert.Equal(
            OrderStatus.Confirmed,
            (await db.Orders.FindAsync(created.Id))!.OrderStatus);
    }

    [Fact]
    public async Task Prepare_deducts_all_stock_once_and_records_audit_rows()
    {
        await using var db = CreateContext();
        AddPrincipals(db);
        db.Products.AddRange(
            Product(1, price: 10m, stock: 5),
            Product(2, price: 20m, stock: 8));
        await db.SaveChangesAsync();
        var service = new OrderService(db, CurrentUser(1, UserRole.Warehouse));
        var created = await service.CreateAsync(
            OrderRequest(
                new OrderItemRequest(1, 3),
                new OrderItemRequest(2, 2)),
            CancellationToken.None);
        await service.ConfirmAsync(created.Id, CancellationToken.None);

        var result = await service.PrepareAsync(created.Id, CancellationToken.None);

        Assert.Equal(OrderStatus.Preparing, result.OrderStatus);
        Assert.Equal(2, (await db.Products.FindAsync(1))!.StockQuantity);
        Assert.Equal(6, (await db.Products.FindAsync(2))!.StockQuantity);
        Assert.Equal(2, await db.InventoryTransactions.CountAsync());
        Assert.All(
            await db.InventoryTransactions.ToListAsync(),
            transaction => Assert.Equal(
                InventoryTransactionType.StockOut,
                transaction.TransactionType));

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.PrepareAsync(created.Id, CancellationToken.None));

        Assert.Equal("invalid_order_status", exception.Code);
        Assert.Equal(2, (await db.Products.FindAsync(1))!.StockQuantity);
        Assert.Equal(6, (await db.Products.FindAsync(2))!.StockQuantity);
        Assert.Equal(2, await db.InventoryTransactions.CountAsync());
    }

    [Fact]
    public async Task Failed_delivery_restocks_once_and_becomes_terminal()
    {
        await using var db = CreateContext();
        AddPrincipals(db);
        db.Products.Add(Product(1, price: 10m, stock: 2));
        db.Orders.Add(Order(
            id: 1,
            status: OrderStatus.Delivering,
            deliveryUserId: 4));
        db.OrderDetails.Add(new OrderDetail
        {
            Id = 1,
            OrderId = 1,
            ProductId = 1,
            Quantity = 3,
            UnitPrice = 10m,
            TotalPrice = 30m
        });
        db.Deliveries.Add(new Delivery
        {
            Id = 1,
            OrderId = 1,
            DeliveryUserId = 4,
            Status = DeliveryStatus.Delivering,
            StartedAt = DateTime.UtcNow.AddHours(-1)
        });
        await db.SaveChangesAsync();
        var service = new DeliveryService(db, CurrentUser(4, UserRole.Delivery));

        var result = await service.FailAsync(
            1,
            new FailDeliveryRequest { FailureReason = "Khách vắng nhà" },
            CancellationToken.None);

        Assert.Equal(DeliveryStatus.Failed, result.Status);
        Assert.Equal(OrderStatus.DeliveryFailed, result.Order!.OrderStatus);
        Assert.Equal("Khách vắng nhà", result.FailureReason);
        Assert.Equal(5, (await db.Products.FindAsync(1))!.StockQuantity);
        var transaction = Assert.Single(await db.InventoryTransactions.ToListAsync());
        Assert.Equal(InventoryTransactionType.StockIn, transaction.TransactionType);
        Assert.Equal(2, transaction.QuantityBefore);
        Assert.Equal(5, transaction.QuantityAfter);

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => service.FailAsync(
                1,
                new FailDeliveryRequest { FailureReason = "Thử lại" },
                CancellationToken.None));

        Assert.Equal("invalid_delivery_status", exception.Code);
        Assert.Equal(5, (await db.Products.FindAsync(1))!.StockQuantity);
        Assert.Single(await db.InventoryTransactions.ToListAsync());

        var completeException = await Assert.ThrowsAsync<BusinessException>(
            () => service.CompleteAsync(
                1,
                new CompleteDeliveryRequest { MarkAsPaid = true },
                CancellationToken.None));

        Assert.Equal("invalid_delivery_status", completeException.Code);
        Assert.Equal(
            OrderStatus.DeliveryFailed,
            (await db.Orders.FindAsync(1))!.OrderStatus);
        Assert.Equal(5, (await db.Products.FindAsync(1))!.StockQuantity);
        Assert.Single(await db.InventoryTransactions.ToListAsync());
    }

    [Fact]
    public async Task Started_delivery_cannot_be_reassigned()
    {
        await using var db = CreateContext();
        AddPrincipals(db);
        db.Users.Add(new User
        {
            Id = 5,
            FullName = "Nhân viên giao hàng khác",
            Username = "delivery-2",
            PasswordHash = "unused",
            Role = UserRole.Delivery
        });
        db.Orders.Add(Order(
            id: 1,
            status: OrderStatus.ReadyForDelivery,
            deliveryUserId: 4));
        db.Deliveries.Add(new Delivery
        {
            Id = 1,
            OrderId = 1,
            DeliveryUserId = 4,
            Status = DeliveryStatus.Assigned
        });
        await db.SaveChangesAsync();

        await new DeliveryService(db, CurrentUser(4, UserRole.Delivery))
            .StartAsync(1, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => new OrderService(db, CurrentUser(1, UserRole.Sales))
                .AssignDeliveryAsync(1, 5, CancellationToken.None));

        Assert.Equal("invalid_order_status", exception.Code);
        Assert.Equal(4, (await db.Orders.FindAsync(1))!.DeliveryUserId);
        Assert.Equal(4, (await db.Deliveries.FindAsync(1))!.DeliveryUserId);
        Assert.Equal(
            DeliveryStatus.Delivering,
            (await db.Deliveries.FindAsync(1))!.Status);
    }

    [Fact]
    public async Task Delivery_action_rejects_a_user_who_is_not_the_assigned_shipper()
    {
        await using var db = CreateContext();
        AddPrincipals(db);
        db.Orders.Add(Order(
            id: 1,
            status: OrderStatus.Delivering,
            deliveryUserId: 4));
        db.Deliveries.Add(new Delivery
        {
            Id = 1,
            OrderId = 1,
            DeliveryUserId = 4,
            Status = DeliveryStatus.Delivering,
            StartedAt = DateTime.UtcNow.AddMinutes(-10)
        });
        await db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => new DeliveryService(db, CurrentUser(1, UserRole.Delivery))
                .CompleteAsync(
                    1,
                    new CompleteDeliveryRequest { MarkAsPaid = true },
                    CancellationToken.None));

        Assert.Equal(StatusCodes.Status403Forbidden, exception.StatusCode);
        Assert.Equal("forbidden_delivery", exception.Code);
        Assert.Equal(
            DeliveryStatus.Delivering,
            (await db.Deliveries.FindAsync(1))!.Status);
        Assert.Equal(
            OrderStatus.Delivering,
            (await db.Orders.FindAsync(1))!.OrderStatus);
    }

    [Fact]
    public async Task Complete_delivery_calculates_container_deposit_and_balance_once()
    {
        await using var db = CreateContext();
        AddPrincipals(db);
        var product = Product(1, price: 20_000m, stock: 5);
        product.RequiresContainer = true;
        product.ContainerDeposit = 50_000m;
        db.Products.Add(product);
        var order = Order(1, OrderStatus.Delivering, 4);
        order.ProductAmount = 60_000m;
        order.TotalAmount = 60_000m;
        db.Orders.Add(order);
        db.OrderDetails.Add(new OrderDetail
        {
            Id = 1,
            OrderId = 1,
            ProductId = 1,
            Quantity = 3,
            UnitPrice = 20_000m,
            TotalPrice = 60_000m
        });
        db.Deliveries.Add(new Delivery
        {
            Id = 1,
            OrderId = 1,
            DeliveryUserId = 4,
            Status = DeliveryStatus.Delivering,
            StartedAt = DateTime.UtcNow.AddMinutes(-10)
        });
        await db.SaveChangesAsync();

        var service = new DeliveryService(db, CurrentUser(4, UserRole.Delivery));
        var result = await service.CompleteAsync(
            1,
            new CompleteDeliveryRequest
            {
                ContainerReturns = [new ContainerReturnRequest { OrderDetailId = 1, Quantity = 1 }]
            },
            CancellationToken.None);

        Assert.Equal(OrderStatus.Delivered, result.Order!.OrderStatus);
        Assert.Equal(100_000m, result.Order.DepositAmount);
        Assert.Equal(160_000m, result.Order.TotalAmount);
        Assert.Equal(160_000m, result.Order.RemainingAmount);
        Assert.Equal(2, (await db.Customers.FindAsync(1))!.ContainerBalance);
        var detail = Assert.Single(result.Order.Details);
        Assert.Equal(1, detail.ReturnedContainerQuantity);
        Assert.Equal(100_000m, detail.ContainerDepositAmount);
        var containerTransaction = Assert.Single(await db.ContainerTransactions.ToListAsync());
        Assert.Equal(0, containerTransaction.BalanceBefore);
        Assert.Equal(2, containerTransaction.BalanceAfter);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.CompleteAsync(1, new CompleteDeliveryRequest(), CancellationToken.None));
        Assert.Equal("invalid_delivery_status", exception.Code);
        Assert.Equal(2, (await db.Customers.FindAsync(1))!.ContainerBalance);
        Assert.Single(await db.ContainerTransactions.ToListAsync());
    }

    [Fact]
    public async Task Record_payment_supports_partial_payment_and_rejects_overpayment()
    {
        await using var db = CreateContext();
        AddPrincipals(db);
        var order = Order(1, OrderStatus.Delivered, 4);
        order.ProductAmount = 100_000m;
        order.TotalAmount = 100_000m;
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        var service = new OrderService(db, CurrentUser(1, UserRole.Sales));

        var partial = await service.RecordPaymentAsync(
            1,
            new RecordPaymentRequest { Amount = 40_000m, PaymentMethod = PaymentMethod.BankTransfer },
            CancellationToken.None);

        Assert.Equal(PaymentStatus.Partial, partial.PaymentStatus);
        Assert.Equal(40_000m, partial.PaidAmount);
        Assert.Equal(60_000m, partial.RemainingAmount);
        Assert.Single(partial.Payments);

        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            service.RecordPaymentAsync(
                1,
                new RecordPaymentRequest { Amount = 60_001m, PaymentMethod = PaymentMethod.Cash },
                CancellationToken.None));
        Assert.Equal("invalid_payment_amount", exception.Code);
        Assert.Single(await db.Payments.ToListAsync());

        var paid = await service.RecordPaymentAsync(
            1,
            new RecordPaymentRequest { Amount = 60_000m, PaymentMethod = PaymentMethod.Cash },
            CancellationToken.None);
        Assert.Equal(PaymentStatus.Paid, paid.PaymentStatus);
        Assert.Equal(0m, paid.RemainingAmount);
        Assert.Equal(2, paid.Payments.Count);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"order-workflow-tests-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings =>
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private static void AddPrincipals(AppDbContext db)
    {
        db.Users.AddRange(
            new User
            {
                Id = 1,
                FullName = "Nhân viên",
                Username = "staff",
                PasswordHash = "unused",
                Role = UserRole.Sales
            },
            new User
            {
                Id = 4,
                FullName = "Nhân viên giao hàng",
                Username = "delivery",
                PasswordHash = "unused",
                Role = UserRole.Delivery
            });
        db.Customers.Add(new Customer
        {
            Id = 1,
            CustomerCode = "KH01",
            CustomerName = "Khách hàng",
            Phone = "0900000000",
            Address = "Hà Nội"
        });
        db.ProductCategories.Add(new ProductCategory
        {
            Id = 1,
            Name = "Nước uống"
        });
    }

    private static Product Product(
        int id,
        decimal price,
        int stock) =>
        new()
        {
            Id = id,
            ProductCode = $"SP{id:00}",
            ProductName = $"Sản phẩm {id}",
            CategoryId = 1,
            Capacity = "20L",
            Unit = "Bình",
            Price = price,
            StockQuantity = stock,
            MinimumStock = 1
        };

    private static Order Order(
        int id,
        OrderStatus status,
        int? deliveryUserId) =>
        new()
        {
            Id = id,
            OrderCode = $"DH{id:00}",
            CustomerId = 1,
            CreatedByUserId = 1,
            DeliveryUserId = deliveryUserId,
            DeliveryAddress = "Hà Nội",
            ProductAmount = 30m,
            TotalAmount = 30m,
            PaymentMethod = PaymentMethod.Cash,
            PaymentStatus = PaymentStatus.Unpaid,
            OrderStatus = status
        };

    private static CreateOrderRequest OrderRequest(
        params OrderItemRequest[] items) =>
        new()
        {
            CustomerId = 1,
            DeliveryAddress = "Hà Nội",
            PaymentMethod = PaymentMethod.Cash,
            Items = [.. items]
        };

    private static ICurrentUser CurrentUser(int id, UserRole role) =>
        new StubCurrentUser(id, role.ToString());

    private sealed record StubCurrentUser(int Id, string Role) : ICurrentUser;
}
