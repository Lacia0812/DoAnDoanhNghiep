using GiaPhuWaterDelivery.Api.Models;

namespace GiaPhuWaterDelivery.Api.Tests.Models;

public sealed class EnumContractTests
{
    [Theory]
    [InlineData(typeof(UserRole), "Admin,Sales,Warehouse,Delivery")]
    [InlineData(typeof(OrderStatus), "New,Confirmed,Preparing,ReadyForDelivery,Delivering,Delivered,DeliveryFailed,Cancelled")]
    [InlineData(typeof(DeliveryStatus), "Pending,Assigned,Delivering,Completed,Failed")]
    [InlineData(typeof(PaymentMethod), "Cash,BankTransfer,CashOnDelivery")]
    [InlineData(typeof(PaymentStatus), "Unpaid,Partial,Paid")]
    [InlineData(typeof(CustomerType), "Individual,Dealer,Company,School,Restaurant")]
    [InlineData(typeof(InventoryTransactionType), "StockIn,StockOut")]
    public void Public_enum_names_are_stable(Type enumType, string expectedNames)
    {
        var expected = expectedNames.Split(',');

        Assert.Equal(expected, Enum.GetNames(enumType));
    }
}
