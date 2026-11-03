using System.ComponentModel.DataAnnotations;
using GiaPhuWaterDelivery.Api.DTOs;
using GiaPhuWaterDelivery.Api.Models;

namespace GiaPhuWaterDelivery.Api.Tests.DTOs;

public sealed class RequestValidationTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public void Order_item_requires_positive_product_and_quantity(
        int productId,
        int quantity)
    {
        var request = new OrderItemRequest(productId, quantity);

        Assert.False(IsValid(request));
    }

    [Fact]
    public void Create_order_requires_at_least_one_item()
    {
        var request = ValidOrder();
        request.Items.Clear();

        Assert.False(IsValid(request));
    }

    [Fact]
    public void Create_order_accepts_a_valid_payload()
    {
        Assert.True(IsValid(ValidOrder()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Add_stock_requires_positive_quantity(int quantity)
    {
        Assert.False(IsValid(new AddStockRequest { Quantity = quantity }));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Failed_delivery_requires_a_reason(string? failureReason)
    {
        var request = new FailDeliveryRequest
        {
            FailureReason = failureReason!
        };

        Assert.False(IsValid(request));
    }

    [Fact]
    public void Password_change_requires_six_characters()
    {
        Assert.False(IsValid(new ChangePasswordRequest { NewPassword = "12345" }));
        Assert.True(IsValid(new ChangePasswordRequest { NewPassword = "123456" }));
    }

    [Fact]
    public void Container_return_requires_positive_quantity()
    {
        Assert.False(IsValid(new ContainerReturnEntryRequest
            { CustomerId = 1, ProductId = 1, Quantity = 0 }));
        Assert.True(IsValid(new ContainerReturnEntryRequest
            { CustomerId = 1, ProductId = 1, Quantity = 2 }));
    }

    [Fact]
    public void Container_adjustment_requires_a_reason()
    {
        Assert.False(IsValid(new ContainerAdjustmentRequest
            { CustomerId = 1, ProductId = 1, QuantityChange = -1, Reason = "" }));
        Assert.True(IsValid(new ContainerAdjustmentRequest
            { CustomerId = 1, ProductId = 1, QuantityChange = -1, Reason = "Kiểm kê" }));
    }

    private static CreateOrderRequest ValidOrder() =>
        new()
        {
            CustomerId = 1,
            DeliveryAddress = "123 Nguyễn Trãi",
            PaymentMethod = PaymentMethod.Cash,
            Items = [new OrderItemRequest(1, 2)]
        };

    private static bool IsValid(object value)
    {
        var validationResults = new List<ValidationResult>();
        return Validator.TryValidateObject(
            value,
            new ValidationContext(value),
            validationResults,
            validateAllProperties: true);
    }
}
