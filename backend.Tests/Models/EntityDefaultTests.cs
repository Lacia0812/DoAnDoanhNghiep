using GiaPhuWaterDelivery.Api.Models;

namespace GiaPhuWaterDelivery.Api.Tests.Models;

public sealed class EntityDefaultTests
{
    [Fact]
    public void Soft_deletable_entities_start_active()
    {
        Assert.True(new User().IsActive);
        Assert.True(new Product().IsActive);
        Assert.True(new Customer().IsActive);
    }

    [Fact]
    public void New_order_has_expected_initial_workflow_state()
    {
        var order = new Order();

        Assert.Equal(OrderStatus.New, order.OrderStatus);
        Assert.Equal(PaymentStatus.Unpaid, order.PaymentStatus);
        Assert.Empty(order.Details);
        Assert.Null(order.Delivery);
    }

    [Fact]
    public void New_delivery_starts_assigned_and_without_timestamps()
    {
        var delivery = new Delivery();

        Assert.Equal(DeliveryStatus.Assigned, delivery.Status);
        Assert.Null(delivery.StartedAt);
        Assert.Null(delivery.CompletedAt);
        Assert.Null(delivery.FailureReason);
    }

    [Theory]
    [MemberData(nameof(UtcTimestampFactories))]
    public void New_entities_use_utc_creation_timestamps(Func<DateTime> timestampFactory)
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var value = timestampFactory();
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.Equal(DateTimeKind.Utc, value.Kind);
        Assert.InRange(value, before, after);
    }

    public static TheoryData<Func<DateTime>> UtcTimestampFactories =>
        new()
        {
            () => new User().CreatedAt,
            () => new Product().CreatedAt,
            () => new Customer().CreatedAt,
            () => new Order().CreatedAt,
            () => new Delivery().AssignedAt,
            () => new InventoryTransaction().CreatedAt
        };
}
