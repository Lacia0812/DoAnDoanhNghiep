using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.Helpers;
using GiaPhuWaterDelivery.Api.Models;
using GiaPhuWaterDelivery.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace GiaPhuWaterDelivery.Api.Tests.Services;

public sealed class DashboardServiceTests
{
    [Fact]
    public async Task Summary_counts_active_entities_and_completed_delivery_revenue()
    {
        await using var db = CreateContext();
        AddRequiredPrincipals(db);
        db.Products.AddRange(
            Product(1, "SP01", stock: 2, minimum: 3),
            Product(2, "SP02", stock: 10, minimum: 3),
            Product(3, "SP03", stock: 0, minimum: 3, isActive: false));

        db.Orders.AddRange(
            Order(1, "DH01", OrderStatus.Delivered, 120_000m, PaymentStatus.Unpaid),
            Order(2, "DH02", OrderStatus.DeliveryFailed, 900_000m),
            Order(3, "DH03", OrderStatus.Cancelled, 800_000m),
            Order(4, "DH04", OrderStatus.New, 100_000m),
            Order(5, "DH05", OrderStatus.Delivering, 200_000m));
        db.Deliveries.AddRange(
            Delivery(1, 1, DeliveryStatus.Completed, DateTime.UtcNow),
            Delivery(2, 2, DeliveryStatus.Failed, DateTime.UtcNow));
        await db.SaveChangesAsync();

        var result = await new DashboardService(db).SummaryAsync(
            CancellationToken.None);

        Assert.Equal(2, result.TotalProducts);
        Assert.Equal(1, result.TotalCustomers);
        Assert.Equal(5, result.TotalOrders);
        Assert.Equal(1, result.NewOrders);
        Assert.Equal(1, result.DeliveringOrders);
        Assert.Equal(1, result.DeliveredOrders);
        Assert.Equal(120_000m, result.MonthlyRevenue);
        Assert.Equal(1, result.LowStockProducts);
    }

    [Fact]
    public async Task Revenue_uses_completed_at_and_returns_empty_months()
    {
        await using var db = CreateContext();
        AddRequiredPrincipals(db);
        var localNow = VietnamTime.Now;
        var currentMonth = new DateTime(localNow.Year, localNow.Month, 15, 12, 0, 0);
        var previousMonth = currentMonth.AddMonths(-1);

        db.Orders.AddRange(
            Order(1, "DH01", OrderStatus.Delivered, 100m),
            Order(2, "DH02", OrderStatus.Delivered, 50m),
            Order(3, "DH03", OrderStatus.DeliveryFailed, 999m));
        db.Deliveries.AddRange(
            Delivery(1, 1, DeliveryStatus.Completed, VietnamTime.ToUtc(currentMonth)),
            Delivery(2, 2, DeliveryStatus.Completed, VietnamTime.ToUtc(previousMonth)),
            Delivery(3, 3, DeliveryStatus.Failed, VietnamTime.ToUtc(currentMonth)));
        await db.SaveChangesAsync();

        var result = (await new DashboardService(db).RevenueAsync(
            months: 3,
            CancellationToken.None)).ToArray();

        Assert.Equal(3, result.Length);
        Assert.Equal(50m, result[^2].Revenue);
        Assert.Equal(100m, result[^1].Revenue);
        Assert.Equal(0m, result[^3].Revenue);
        Assert.Equal(previousMonth.ToString("yyyy-MM"), result[^2].Month);
        Assert.Equal(currentMonth.ToString("yyyy-MM"), result[^1].Month);
    }

    [Fact]
    public async Task Order_status_chart_contains_every_status_including_zero_counts()
    {
        await using var db = CreateContext();
        AddRequiredPrincipals(db);
        db.Orders.AddRange(
            Order(1, "DH01", OrderStatus.New, 10m),
            Order(2, "DH02", OrderStatus.New, 20m),
            Order(3, "DH03", OrderStatus.Cancelled, 30m));
        await db.SaveChangesAsync();

        var result = await new DashboardService(db).OrderStatusAsync(
            CancellationToken.None);

        Assert.Equal(Enum.GetValues<OrderStatus>().Length, result.Count);
        Assert.Equal(2, result.Single(x => x.Status == OrderStatus.New).Count);
        Assert.Equal(1, result.Single(x => x.Status == OrderStatus.Cancelled).Count);
        Assert.Equal(0, result.Single(x => x.Status == OrderStatus.Delivered).Count);
    }

    [Fact]
    public async Task Top_products_uses_only_delivered_orders()
    {
        await using var db = CreateContext();
        AddRequiredPrincipals(db);
        db.Products.AddRange(
            Product(1, "SP01", name: "Nước 20L"),
            Product(2, "SP02", name: "Nước 500ml"));
        db.Orders.AddRange(
            Order(1, "DH01", OrderStatus.Delivered, 100m),
            Order(2, "DH02", OrderStatus.DeliveryFailed, 1_000m),
            Order(3, "DH03", OrderStatus.Cancelled, 1_000m));
        db.OrderDetails.AddRange(
            Detail(1, 1, 1, quantity: 4, totalPrice: 80m),
            Detail(2, 1, 2, quantity: 2, totalPrice: 20m),
            Detail(3, 2, 2, quantity: 100, totalPrice: 1_000m),
            Detail(4, 3, 2, quantity: 100, totalPrice: 1_000m));
        await db.SaveChangesAsync();

        var result = (await new DashboardService(db).TopProductsAsync(
            take: 5,
            CancellationToken.None)).ToArray();

        Assert.Equal(2, result.Length);
        Assert.Equal(1, result[0].ProductId);
        Assert.Equal(4, result[0].QuantitySold);
        Assert.Equal(80m, result[0].Revenue);
        Assert.Equal(2, result[1].QuantitySold);
    }

    [Fact]
    public async Task Restock_uses_thirty_day_window_and_handles_boundary_cases()
    {
        await using var db = CreateContext();
        AddRequiredPrincipals(db);
        db.Products.AddRange(
            Product(1, "BOUNDARY", stock: 7, minimum: 0),
            Product(2, "NO-SALES", stock: 3, minimum: 5),
            Product(3, "OLD-SALES", stock: 1, minimum: 0),
            Product(4, "FAILED", stock: 1, minimum: 0),
            Product(5, "ABOVE", stock: 8, minimum: 0));
        db.Orders.AddRange(
            Order(1, "RECENT-1", OrderStatus.Delivered, 30m),
            Order(2, "OLD-1", OrderStatus.Delivered, 300m),
            Order(3, "FAILED-1", OrderStatus.DeliveryFailed, 300m),
            Order(4, "RECENT-2", OrderStatus.Delivered, 30m));
        db.OrderDetails.AddRange(
            Detail(1, 1, 1, quantity: 30, totalPrice: 30m),
            Detail(2, 2, 3, quantity: 300, totalPrice: 300m),
            Detail(3, 3, 4, quantity: 300, totalPrice: 300m),
            Detail(4, 4, 5, quantity: 30, totalPrice: 30m));
        db.Deliveries.AddRange(
            Delivery(1, 1, DeliveryStatus.Completed, DateTime.UtcNow.AddDays(-1)),
            Delivery(2, 2, DeliveryStatus.Completed, DateTime.UtcNow.AddDays(-31)),
            Delivery(3, 3, DeliveryStatus.Failed, DateTime.UtcNow.AddDays(-1)),
            Delivery(4, 4, DeliveryStatus.Completed, DateTime.UtcNow.AddDays(-1)));
        await db.SaveChangesAsync();

        var result = (await new DashboardService(db).RestockSuggestionsAsync(
            CancellationToken.None)).ToDictionary(x => x.ProductCode);

        Assert.Equal(1m, result["BOUNDARY"].AverageDailySales);
        Assert.Equal(7m, result["BOUNDARY"].DaysRemaining);
        Assert.True(result["BOUNDARY"].NeedsRestock);

        Assert.Equal(0m, result["NO-SALES"].AverageDailySales);
        Assert.Null(result["NO-SALES"].DaysRemaining);
        Assert.False(result["NO-SALES"].NeedsRestock);
        Assert.True(result["NO-SALES"].IsBelowMinimum);

        Assert.Null(result["OLD-SALES"].DaysRemaining);
        Assert.Null(result["FAILED"].DaysRemaining);
        Assert.Equal(8m, result["ABOVE"].DaysRemaining);
        Assert.False(result["ABOVE"].NeedsRestock);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"dashboard-tests-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private static void AddRequiredPrincipals(AppDbContext db)
    {
        db.Users.Add(new User
        {
            Id = 1,
            FullName = "Test User",
            Username = "test",
            PasswordHash = "unused",
            Role = UserRole.Admin
        });
        db.Customers.AddRange(
            new Customer
            {
                Id = 1,
                CustomerCode = "KH01",
                CustomerName = "Khách hoạt động",
                Phone = "0900000000",
                Address = "Hà Nội",
                IsActive = true
            },
            new Customer
            {
                Id = 2,
                CustomerCode = "KH02",
                CustomerName = "Khách ngừng hoạt động",
                Phone = "0900000001",
                Address = "Hà Nội",
                IsActive = false
            });
        db.ProductCategories.Add(new ProductCategory
        {
            Id = 1,
            Name = "Test category"
        });
    }

    private static Product Product(
        int id,
        string code,
        string? name = null,
        int stock = 100,
        int minimum = 5,
        bool isActive = true) =>
        new()
        {
            Id = id,
            ProductCode = code,
            ProductName = name ?? code,
            CategoryId = 1,
            Capacity = "20L",
            Unit = "Bình",
            Price = 10m,
            StockQuantity = stock,
            MinimumStock = minimum,
            IsActive = isActive
        };

    private static Order Order(
        int id,
        string code,
        OrderStatus status,
        decimal amount,
        PaymentStatus paymentStatus = PaymentStatus.Paid) =>
        new()
        {
            Id = id,
            OrderCode = code,
            CustomerId = 1,
            CreatedByUserId = 1,
            DeliveryAddress = "Hà Nội",
            TotalAmount = amount,
            PaymentMethod = PaymentMethod.Cash,
            PaymentStatus = paymentStatus,
            OrderStatus = status
        };

    private static Delivery Delivery(
        int id,
        int orderId,
        DeliveryStatus status,
        DateTime completedAt) =>
        new()
        {
            Id = id,
            OrderId = orderId,
            DeliveryUserId = 1,
            Status = status,
            CompletedAt = completedAt
        };

    private static OrderDetail Detail(
        int id,
        int orderId,
        int productId,
        int quantity,
        decimal totalPrice) =>
        new()
        {
            Id = id,
            OrderId = orderId,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = totalPrice / quantity,
            TotalPrice = totalPrice
        };
}
