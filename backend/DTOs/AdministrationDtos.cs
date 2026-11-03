using System.ComponentModel.DataAnnotations;

namespace GiaPhuWaterDelivery.Api.DTOs;

public sealed class ContainerReturnEntryRequest
{
    [Range(1, int.MaxValue)] public int CustomerId { get; set; }
    [Range(1, int.MaxValue)] public int ProductId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
    [MaxLength(1000)] public string? Note { get; set; }
}

public sealed class ContainerAdjustmentRequest
{
    [Range(1, int.MaxValue)] public int CustomerId { get; set; }
    [Range(1, int.MaxValue)] public int ProductId { get; set; }
    [Range(-1000000, 1000000)] public int QuantityChange { get; set; }
    [Required, MinLength(3), MaxLength(1000)] public string Reason { get; set; } = string.Empty;
}

public sealed record AuditLogDto(
    long Id,
    int? UserId,
    string ActorName,
    string Action,
    string EntityType,
    string? EntityId,
    string? OldValues,
    string? NewValues,
    string? IpAddress,
    DateTime CreatedAt);
