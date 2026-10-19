using GiaPhuWaterDelivery.Api.Helpers;
using GiaPhuWaterDelivery.Api.Models;
using Microsoft.AspNetCore.Http;

namespace GiaPhuWaterDelivery.Api.Tests.Helpers;

public sealed class OrderStateMachineTests
{
    private static readonly HashSet<(OrderStatus From, OrderStatus To)> AllowedTransitions =
    [
        (OrderStatus.New, OrderStatus.Confirmed),
        (OrderStatus.New, OrderStatus.Cancelled),
        (OrderStatus.Confirmed, OrderStatus.Preparing),
        (OrderStatus.Preparing, OrderStatus.ReadyForDelivery),
        (OrderStatus.ReadyForDelivery, OrderStatus.Delivering),
        (OrderStatus.Delivering, OrderStatus.Delivered),
        (OrderStatus.Delivering, OrderStatus.DeliveryFailed)
    ];

    public static TheoryData<OrderStatus, OrderStatus> ValidTransitions =>
        CreateCases(allowed: true);

    public static TheoryData<OrderStatus, OrderStatus> InvalidTransitions =>
        CreateCases(allowed: false);

    [Theory]
    [MemberData(nameof(ValidTransitions))]
    public void CanTransition_accepts_only_the_locked_workflow(
        OrderStatus from,
        OrderStatus to)
    {
        Assert.True(OrderStateMachine.CanTransition(from, to));
    }

    [Theory]
    [MemberData(nameof(InvalidTransitions))]
    public void CanTransition_rejects_every_other_state_pair(
        OrderStatus from,
        OrderStatus to)
    {
        Assert.False(OrderStateMachine.CanTransition(from, to));
    }

    [Theory]
    [MemberData(nameof(ValidTransitions))]
    public void Ensure_does_not_throw_for_valid_transitions(
        OrderStatus from,
        OrderStatus to)
    {
        var exception = Record.Exception(() => OrderStateMachine.Ensure(from, to));

        Assert.Null(exception);
    }

    [Theory]
    [MemberData(nameof(InvalidTransitions))]
    public void Ensure_returns_conflict_for_invalid_transitions(
        OrderStatus from,
        OrderStatus to)
    {
        var exception = Assert.Throws<BusinessException>(
            () => OrderStateMachine.Ensure(from, to));

        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);
        Assert.Equal("invalid_order_status", exception.Code);
        Assert.Contains(from.ToString(), exception.Message);
        Assert.Contains(to.ToString(), exception.Message);
    }

    private static TheoryData<OrderStatus, OrderStatus> CreateCases(bool allowed)
    {
        var data = new TheoryData<OrderStatus, OrderStatus>();

        foreach (var from in Enum.GetValues<OrderStatus>())
        foreach (var to in Enum.GetValues<OrderStatus>())
        {
            if (AllowedTransitions.Contains((from, to)) == allowed)
                data.Add(from, to);
        }

        return data;
    }
}
