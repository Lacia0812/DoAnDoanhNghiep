using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.DTOs;
using GiaPhuWaterDelivery.Api.Helpers;
using GiaPhuWaterDelivery.Api.Models;
using GiaPhuWaterDelivery.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GiaPhuWaterDelivery.Api.Tests.Services;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task Login_normalizes_username_and_returns_expected_jwt_claims()
    {
        await using var db = CreateContext();
        db.Users.Add(User(
            id: 7,
            username: "sales",
            password: "GiaPhu@123",
            role: UserRole.Sales));
        await db.SaveChangesAsync();

        var result = await CreateService(db).LoginAsync(
            new LoginRequest
            {
                Username = "  SALES  ",
                Password = "GiaPhu@123"
            },
            CancellationToken.None);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Equal(7, result.User.Id);
        Assert.Equal("sales", result.User.Username);
        Assert.Equal(UserRole.Sales, result.User.Role);
        Assert.Contains(token.Claims, claim =>
            claim.Type is JwtRegisteredClaimNames.Sub or ClaimTypes.NameIdentifier &&
            claim.Value == "7");
        Assert.Contains(token.Claims, claim =>
            claim.Type is "role" or ClaimTypes.Role &&
            claim.Value == UserRole.Sales.ToString());
        Assert.InRange(
            result.ExpiresAt,
            DateTime.UtcNow.AddMinutes(29),
            DateTime.UtcNow.AddMinutes(31));
    }

    [Theory]
    [InlineData("wrong-password", true)]
    [InlineData("GiaPhu@123", false)]
    public async Task Login_rejects_wrong_password_and_inactive_user(
        string password,
        bool isActive)
    {
        await using var db = CreateContext();
        db.Users.Add(User(
            id: 1,
            username: "admin",
            password: "GiaPhu@123",
            role: UserRole.Admin,
            isActive));
        await db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => CreateService(db).LoginAsync(
                new LoginRequest { Username = "admin", Password = password },
                CancellationToken.None));

        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCode);
        Assert.Equal("invalid_credentials", exception.Code);
    }

    [Fact]
    public async Task Profile_rejects_an_inactive_user()
    {
        await using var db = CreateContext();
        db.Users.Add(User(
            id: 1,
            username: "admin",
            password: "unused",
            role: UserRole.Admin,
            isActive: false));
        await db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BusinessException>(
            () => CreateService(db).ProfileAsync(1, CancellationToken.None));

        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCode);
        Assert.Equal("inactive_user", exception.Code);
    }

    private static AuthService CreateService(AppDbContext db)
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "tests-only-signing-key-with-at-least-32-bytes",
            ["Jwt:Issuer"] = "GiaPhu.Tests",
            ["Jwt:Audience"] = "GiaPhu.Tests",
            ["Jwt:ExpireMinutes"] = "30"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        return new AuthService(db, configuration);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"auth-tests-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }

    private static User User(
        int id,
        string username,
        string password,
        UserRole role,
        bool isActive = true) =>
        new()
        {
            Id = id,
            FullName = "Test User",
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4),
            Role = role,
            IsActive = isActive
        };
}
