using System.Security.Claims;

namespace GiaPhuWaterDelivery.Api.Helpers;

public interface ICurrentUser
{
    int Id { get; }
    string Role { get; }
}

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User
        ?? throw new BusinessException(StatusCodes.Status401Unauthorized, "Bạn chưa đăng nhập.");

    public int Id
    {
        get
        {
            var value = Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id)
                ? id
                : throw new BusinessException(StatusCodes.Status401Unauthorized, "Token không hợp lệ.");
        }
    }

    public string Role => Principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
}
