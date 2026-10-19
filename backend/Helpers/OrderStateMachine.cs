using GiaPhuWaterDelivery.Api.Models;

namespace GiaPhuWaterDelivery.Api.Helpers;

public static class OrderStateMachine
{
    public static bool CanTransition(OrderStatus from, OrderStatus to) => (from, to) switch
    {
        (OrderStatus.New, OrderStatus.Confirmed) => true,
        (OrderStatus.New, OrderStatus.Cancelled) => true,
        (OrderStatus.Confirmed, OrderStatus.Preparing) => true,
        (OrderStatus.Preparing, OrderStatus.ReadyForDelivery) => true,
        (OrderStatus.ReadyForDelivery, OrderStatus.Delivering) => true,
        (OrderStatus.Delivering, OrderStatus.Delivered) => true,
        (OrderStatus.Delivering, OrderStatus.DeliveryFailed) => true,
        _ => false
    };

    public static void Ensure(OrderStatus from, OrderStatus to)
    {
        if (!CanTransition(from, to))
            throw new BusinessException(
                StatusCodes.Status409Conflict,
                $"Không thể chuyển trạng thái đơn hàng từ {from} sang {to}.",
                "invalid_order_status");
    }
}
