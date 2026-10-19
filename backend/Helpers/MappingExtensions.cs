using GiaPhuWaterDelivery.Api.DTOs;
using GiaPhuWaterDelivery.Api.Models;

namespace GiaPhuWaterDelivery.Api.Helpers;

public static class MappingExtensions
{
    public static UserDto ToDto(this User value) =>
        new(value.Id, value.FullName, value.Username, value.Role, value.IsActive, value.CreatedAt);

    public static AuthUserDto ToAuthDto(this User value) =>
        new(value.Id, value.FullName, value.Username, value.Role, value.IsActive, value.CreatedAt);

    public static ProductDto ToDto(this Product value) =>
        new(value.Id, value.ProductCode, value.ProductName, value.CategoryId, value.Category.Name,
            value.Capacity, value.Unit, value.Price, value.RequiresContainer, value.ContainerDeposit,
            value.StockQuantity, value.MinimumStock,
            value.StockQuantity < value.MinimumStock, value.IsActive, value.CreatedAt);

    public static CustomerDto ToDto(this Customer value) =>
        new(value.Id, value.CustomerCode, value.CustomerName, value.Phone, value.Email, value.Address,
            value.CustomerType, value.ContainerBalance,
            value.Orders.Where(x => x.OrderStatus == OrderStatus.Delivered)
                .Sum(x => x.TotalAmount - x.PaidAmount),
            value.Note, value.IsActive, value.CreatedAt);

    public static OrderDto ToDto(this Order value)
    {
        var details = value.Details
            .OrderBy(x => x.Id)
            .Select(x => new OrderDetailDto(
                x.Id, x.ProductId, x.Product.ProductCode, x.Product.ProductName,
                x.Quantity, x.UnitPrice, x.TotalPrice, x.Product.RequiresContainer,
                x.Product.ContainerDeposit, x.ReturnedContainerQuantity,
                x.ContainerDepositAmount))
            .ToArray();

        var payments = value.Payments
            .OrderByDescending(x => x.PaidAt)
            .Select(x => new PaymentDto(
                x.Id, x.Amount, x.PaymentMethod, x.PaidAt,
                x.RecordedByUserId, x.RecordedByUser.FullName, x.Note))
            .ToArray();

        var delivery = value.Delivery is null
            ? null
            : new DeliverySummaryDto(
                value.Delivery.Id,
                value.Delivery.DeliveryUserId,
                value.Delivery.DeliveryUser.FullName,
                value.Delivery.AssignedAt,
                value.Delivery.StartedAt,
                value.Delivery.CompletedAt,
                value.Delivery.Status,
                value.Delivery.FailureReason,
                value.Delivery.Note);

        return new OrderDto(
            value.Id, value.OrderCode, value.CustomerId, value.Customer.CustomerName,
            value.Customer.Phone,
            value.CreatedByUserId, value.CreatedByUser.FullName,
            value.DeliveryUserId, value.DeliveryUser?.FullName,
            value.OrderDate, value.ExpectedDeliveryDate, value.DeliveryAddress,
            value.ProductAmount, value.DepositAmount, value.TotalAmount,
            value.PaidAmount, value.TotalAmount - value.PaidAmount,
            value.PaymentMethod, value.PaymentStatus, value.OrderStatus,
            value.Note, value.CreatedAt, details, payments, delivery);
    }

    public static DeliveryDto ToDto(this Delivery value, bool includeOrder = false) =>
        new(value.Id, value.OrderId, value.Order.OrderCode,
            value.Order.Customer.CustomerName, value.Order.Customer.Phone,
            value.Order.DeliveryAddress, value.Order.TotalAmount, value.DeliveryUserId,
            value.DeliveryUser.FullName, value.AssignedAt, value.StartedAt, value.CompletedAt,
            value.Status, value.FailureReason, value.Note,
            includeOrder ? value.Order.ToDto() : null);
}
