using System.ComponentModel.DataAnnotations;
using GiaPhuWaterDelivery.Api.Models;

namespace GiaPhuWaterDelivery.Api.DTOs;

public sealed class OrderItemRequest
{
    public OrderItemRequest() { }

    public OrderItemRequest(int productId, int quantity)
    {
        ProductId = productId;
        Quantity = quantity;
    }

    [Range(1, int.MaxValue)] public int ProductId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
}

public class CreateOrderRequest
{
    [Range(1, int.MaxValue)] public int CustomerId { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    [Required, MaxLength(500)] public string DeliveryAddress { get; set; } = string.Empty;
    [Required] public PaymentMethod? PaymentMethod { get; set; }
    [MaxLength(1000)] public string? Note { get; set; }
    [Required, MinLength(1)] public List<OrderItemRequest> Items { get; set; } = [];
}

public sealed class UpdateOrderRequest : CreateOrderRequest;

public sealed record OrderDetailDto(
    int Id,
    int ProductId,
    string ProductCode,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    bool RequiresContainer,
    decimal ContainerDeposit,
    int ReturnedContainerQuantity,
    decimal ContainerDepositAmount);

public sealed record PaymentDto(
    int Id,
    decimal Amount,
    PaymentMethod PaymentMethod,
    DateTime PaidAt,
    int RecordedByUserId,
    string RecordedByName,
    string? Note);

public sealed record DeliverySummaryDto(
    int Id,
    int DeliveryUserId,
    string DeliveryUserName,
    DateTime AssignedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DeliveryStatus Status,
    string? FailureReason,
    string? Note);

public sealed record OrderDto(
    int Id,
    string OrderCode,
    int CustomerId,
    string CustomerName,
    string CustomerPhone,
    int CreatedByUserId,
    string CreatedByName,
    int? DeliveryUserId,
    string? DeliveryUserName,
    DateTime OrderDate,
    DateTime? ExpectedDeliveryDate,
    string DeliveryAddress,
    decimal ProductAmount,
    decimal DepositAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    PaymentMethod PaymentMethod,
    PaymentStatus PaymentStatus,
    OrderStatus OrderStatus,
    string? Note,
    DateTime CreatedAt,
    IReadOnlyCollection<OrderDetailDto> Details,
    IReadOnlyCollection<PaymentDto> Payments,
    DeliverySummaryDto? Delivery);

public sealed class AssignDeliveryRequest
{
    [Range(1, int.MaxValue)] public int DeliveryUserId { get; set; }
}

public sealed class RecordPaymentRequest
{
    [Range(typeof(decimal), "0.01", "9999999999999999")] public decimal Amount { get; set; }
    [Required] public PaymentMethod? PaymentMethod { get; set; }
    [MaxLength(1000)] public string? Note { get; set; }
}
