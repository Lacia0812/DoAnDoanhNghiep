using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiaPhuWaterDelivery.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = "Admin")]
public sealed class AuditLogsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<AuditLogDto>> GetAll(
        int page = 1, int pageSize = 10, string? search = null,
        string? action = null, string? entityType = null,
        DateTime? from = null, DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.ActorName.Contains(term)
                || x.Action.Contains(term) || x.EntityType.Contains(term)
                || (x.EntityId != null && x.EntityId.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(x => x.Action == action.Trim());
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(x => x.EntityType == entityType.Trim());
        if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value.ToUniversalTime());
        if (to.HasValue) query = query.Where(x => x.CreatedAt <= to.Value.ToUniversalTime());
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new AuditLogDto(x.Id, x.UserId, x.ActorName, x.Action,
                x.EntityType, x.EntityId, x.OldValues, x.NewValues, x.IpAddress, x.CreatedAt))
            .ToArrayAsync(cancellationToken);
        return PagedResult<AuditLogDto>.Create(items, count, page, pageSize);
    }
}
