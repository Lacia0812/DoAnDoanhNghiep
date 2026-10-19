using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.DTOs;
using GiaPhuWaterDelivery.Api.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GiaPhuWaterDelivery.Api.Services;

public sealed class AuthService(AppDbContext db, IConfiguration configuration)
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(
            x => x.Username == username && x.IsActive, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new BusinessException(StatusCodes.Status401Unauthorized,
                "Tên đăng nhập hoặc mật khẩu không đúng.", "invalid_credentials");

        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key chưa được cấu hình.");
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var minutes = configuration.GetValue("Jwt:ExpireMinutes", 480);
        var expiresAt = DateTime.UtcNow.AddMinutes(minutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.GivenName, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer, audience, claims, expires: expiresAt, signingCredentials: credentials);

        return new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            user.ToAuthDto());
    }

    public async Task<AuthUserDto> ProfileAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == userId && x.IsActive, cancellationToken)
            ?? throw new BusinessException(StatusCodes.Status401Unauthorized,
                "Tài khoản không tồn tại hoặc đã bị khóa.", "inactive_user");
        return user.ToAuthDto();
    }
}
