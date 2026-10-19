using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GiaPhuWaterDelivery.Api.Tests.Data;

public sealed class AppDbContextModelTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=GiaPhuModelTests;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new AppDbContext(options);
    }

    [Theory]
    [InlineData(typeof(User), nameof(User.Username))]
    [InlineData(typeof(ProductCategory), nameof(ProductCategory.Name))]
    [InlineData(typeof(Product), nameof(Product.ProductCode))]
    [InlineData(typeof(Customer), nameof(Customer.CustomerCode))]
    [InlineData(typeof(Order), nameof(Order.OrderCode))]
    [InlineData(typeof(Delivery), nameof(Delivery.OrderId))]
    public void Required_business_keys_have_unique_indexes(Type entityType, string propertyName)
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(entityType);

        var index = Assert.Single(
            entity!.GetIndexes(),
            candidate => candidate.Properties.Select(property => property.Name)
                .SequenceEqual([propertyName]));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public void Order_detail_prevents_duplicate_products_in_an_order()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(OrderDetail));

        var index = Assert.Single(
            entity!.GetIndexes(),
            candidate => candidate.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(OrderDetail.OrderId), nameof(OrderDetail.ProductId)]));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public void Delivery_is_configured_as_one_to_one_with_order()
    {
        using var context = CreateContext();
        var delivery = context.Model.FindEntityType(typeof(Delivery));
        var foreignKey = Assert.Single(
            delivery!.GetForeignKeys(),
            candidate => candidate.PrincipalEntityType.ClrType == typeof(Order));

        Assert.True(foreignKey.IsUnique);
        Assert.Equal(nameof(Delivery.OrderId), Assert.Single(foreignKey.Properties).Name);
    }

    [Theory]
    [InlineData(typeof(Order), nameof(Order.CreatedByUserId))]
    [InlineData(typeof(Order), nameof(Order.DeliveryUserId))]
    [InlineData(typeof(Delivery), nameof(Delivery.DeliveryUserId))]
    [InlineData(typeof(OrderDetail), nameof(OrderDetail.ProductId))]
    [InlineData(typeof(InventoryTransaction), nameof(InventoryTransaction.ProductId))]
    [InlineData(typeof(InventoryTransaction), nameof(InventoryTransaction.OrderId))]
    [InlineData(typeof(InventoryTransaction), nameof(InventoryTransaction.CreatedByUserId))]
    public void Historical_foreign_keys_use_restrict_delete(Type dependentType, string foreignKeyProperty)
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(dependentType);

        var foreignKey = Assert.Single(
            entity!.GetForeignKeys(),
            candidate => candidate.Properties.Any(property => property.Name == foreignKeyProperty));

        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    [Theory]
    [InlineData(typeof(Product), nameof(Product.Price))]
    [InlineData(typeof(Order), nameof(Order.TotalAmount))]
    [InlineData(typeof(OrderDetail), nameof(OrderDetail.UnitPrice))]
    [InlineData(typeof(OrderDetail), nameof(OrderDetail.TotalPrice))]
    public void Money_columns_use_decimal_18_2(Type entityType, string propertyName)
    {
        using var context = CreateContext();
        var property = context.Model.FindEntityType(entityType)!.FindProperty(propertyName);

        Assert.Equal("decimal(18,2)", property!.GetColumnType());
    }

    [Theory]
    [InlineData(typeof(User), nameof(User.Role))]
    [InlineData(typeof(Customer), nameof(Customer.CustomerType))]
    [InlineData(typeof(Order), nameof(Order.PaymentMethod))]
    [InlineData(typeof(Order), nameof(Order.PaymentStatus))]
    [InlineData(typeof(Order), nameof(Order.OrderStatus))]
    [InlineData(typeof(Delivery), nameof(Delivery.Status))]
    [InlineData(typeof(InventoryTransaction), nameof(InventoryTransaction.TransactionType))]
    public void Enums_are_stored_as_strings(Type entityType, string propertyName)
    {
        using var context = CreateContext();
        var property = context.Model.FindEntityType(entityType)!.FindProperty(propertyName);

        Assert.Equal(typeof(string), property!.GetProviderClrType());
    }
}
