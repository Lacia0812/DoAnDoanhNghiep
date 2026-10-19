using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.DTOs;
using GiaPhuWaterDelivery.Api.Helpers;
using GiaPhuWaterDelivery.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiaPhuWaterDelivery.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(AppDbContext db, ICurrentUser currentUser) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<PagedResult<UserDto>> GetAll(
        int page = 1, int pageSize = 10, string? search = null,
        UserRole? role = null, bool? isActive = null, bool includeInactive = true,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Users.AsNoTracking().AsQueryable();
        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);
        else if (!includeInactive)
            query = query.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.FullName.Contains(term) || x.Username.Contains(term));
        }
        if (role.HasValue)
            query = query.Where(x => x.Role == role);
        var count = await query.CountAsync(cancellationToken);
        var entities = await query.OrderBy(x => x.FullName)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PagedResult<UserDto>.Create(
            entities.Select(x => x.ToDto()).ToArray(), count, page, pageSize);
    }

    [Authorize(Roles = "Admin,Sales")]
    [HttpGet("delivery-staff")]
    public async Task<IReadOnlyCollection<UserDto>> DeliveryStaff(
        CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking()
            .Where(x => x.IsActive && x.Role == UserRole.Delivery)
            .OrderBy(x => x.FullName)
            .Select(x => new UserDto(
                x.Id, x.FullName, x.Username, x.Role, x.IsActive, x.CreatedAt))
            .ToListAsync(cancellationToken);

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:int}")]
    public async Task<UserDto> Get(int id, CancellationToken cancellationToken) =>
        (await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
         ?? throw UserNotFound()).ToDto();

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(
        CreateUserRequest request, CancellationToken cancellationToken)
    {
        var role = request.Role ?? throw InvalidRole();
        var username = request.Username.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Username == username, cancellationToken))
            throw DuplicateUsername();
        var entity = new User
        {
            FullName = request.FullName.Trim(),
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = entity.Id }, entity.ToDto());
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<UserDto> Update(
        int id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var role = request.Role ?? throw InvalidRole();
        var entity = await db.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw UserNotFound();
        var username = request.Username.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(
                x => x.Id != id && x.Username == username, cancellationToken))
            throw DuplicateUsername();
        if (entity.Id == currentUser.Id && !request.IsActive)
            throw new BusinessException(StatusCodes.Status409Conflict,
                "Bạn không thể tự khóa tài khoản đang đăng nhập.", "cannot_deactivate_self");
        entity.FullName = request.FullName.Trim();
        entity.Username = username;
        entity.Role = role;
        entity.IsActive = request.IsActive;
        await db.SaveChangesAsync(cancellationToken);
        return entity.ToDto();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (id == currentUser.Id)
            throw new BusinessException(StatusCodes.Status409Conflict,
                "Bạn không thể tự khóa tài khoản đang đăng nhập.", "cannot_deactivate_self");
        var entity = await db.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw UserNotFound();
        entity.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}/password")]
    public async Task<MessageResponse> ChangePassword(
        int id, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw UserNotFound();
        entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await db.SaveChangesAsync(cancellationToken);
        return new MessageResponse("Đổi mật khẩu thành công.");
    }

    private static BusinessException UserNotFound() =>
        new(StatusCodes.Status404NotFound, "Không tìm thấy tài khoản.", "user_not_found");
    private static BusinessException DuplicateUsername() =>
        new(StatusCodes.Status409Conflict, "Tên đăng nhập đã tồn tại.", "duplicate_username");
    private static BusinessException InvalidRole() =>
        new(StatusCodes.Status400BadRequest, "Vai trò là bắt buộc.", "role_required");
}
