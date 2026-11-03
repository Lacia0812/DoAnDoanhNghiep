using System.Text.Json;
using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.Models;

namespace GiaPhuWaterDelivery.Api.Helpers;

public static class AuditTrail
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void Add(
        AppDbContext db,
        ICurrentUser currentUser,
        string action,
        string entityType,
        object? entityId = null,
        object? oldValues = null,
        object? newValues = null,
        string? ipAddress = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = currentUser.Id,
            ActorName = currentUser.DisplayName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId?.ToString(),
            OldValues = Serialize(oldValues),
            NewValues = Serialize(newValues),
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static string? Serialize(object? value) =>
        value is null ? null : JsonSerializer.Serialize(value, JsonOptions);
}
