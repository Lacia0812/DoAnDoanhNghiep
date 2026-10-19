using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.Models;
using GiaPhuWaterDelivery.Api.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

namespace GiaPhuWaterDelivery.Api.Tests.Services;

public sealed class InvoiceServiceTests
{
    [Fact]
    public async Task Generate_creates_a_pdf_with_order_financials()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"invoice-tests-{Guid.NewGuid():N}")
            .Options;
        await using var db = new AppDbContext(options);
        db.Customers.Add(new Customer
        {
            Id = 1,
            CustomerCode = "KH01",
            CustomerName = "Công ty kiểm thử",
            Phone = "0900000000",
            Address = "Thủ Dầu Một"
        });
        db.ProductCategories.Add(new ProductCategory { Id = 1, Name = "Bình nước" });
        db.Products.Add(new Product
        {
            Id = 1,
            ProductCode = "SP01",
            ProductName = "Bình nước 20L",
            CategoryId = 1,
            Capacity = "20L",
            Unit = "Bình",
            Price = 50_000m,
            RequiresContainer = true,
            ContainerDeposit = 50_000m
        });
        db.Orders.Add(new Order
        {
            Id = 1,
            OrderCode = "DH-PDF-01",
            CustomerId = 1,
            CreatedByUserId = 1,
            DeliveryAddress = "Thủ Dầu Một",
            ProductAmount = 100_000m,
            DepositAmount = 50_000m,
            TotalAmount = 150_000m,
            PaidAmount = 50_000m,
            PaymentMethod = PaymentMethod.BankTransfer,
            PaymentStatus = PaymentStatus.Partial,
            OrderStatus = OrderStatus.Delivered
        });
        db.OrderDetails.Add(new OrderDetail
        {
            Id = 1,
            OrderId = 1,
            ProductId = 1,
            Quantity = 2,
            ReturnedContainerQuantity = 1,
            UnitPrice = 50_000m,
            TotalPrice = 100_000m,
            ContainerDepositAmount = 50_000m
        });
        await db.SaveChangesAsync();

        var result = await new InvoiceService(db).GenerateAsync(1, CancellationToken.None);

        Assert.Equal("HoaDon-DH-PDF-01.pdf", result.FileName);
        Assert.True(result.Content.Length > 1_000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(result.Content, 0, 4));
    }
}
