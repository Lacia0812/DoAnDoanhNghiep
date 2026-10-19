using System.ComponentModel.DataAnnotations;
using GiaPhuWaterDelivery.Api.Models;

namespace GiaPhuWaterDelivery.Api.DTOs;

public sealed record DeliveryDto(
    int Id,
    int OrderId,
    string OrderCode,
    string CustomerName,
    string CustomerPhone,
    string DeliveryAddress,
    decimal TotalAmount,
    int DeliveryUserId,
    string DeliveryUserName,
    DateTime AssignedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DeliveryStatus Status,
    string? FailureReason,
    string? Note,
    OrderDto? Order);

public sealed class CompleteDeliveryRequest
{
    public bool MarkAsPaid { get; set; }
    public List<ContainerReturnRequest> ContainerReturns { get; set; } = [];
    [MaxLength(1000)] public string? Note { get; set; }
}

public sealed class ContainerReturnRequest
{
    [Range(1, int.MaxValue)] public int OrderDetailId { get; set; }
    [Range(0, int.MaxValue)] public int Quantity { get; set; }
}

public sealed class FailDeliveryRequest
{
    [Required, MaxLength(1000)] public string FailureReason { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Note { get; set; }
}
