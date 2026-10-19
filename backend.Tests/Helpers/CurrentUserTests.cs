using System.Security.Claims;
using GiaPhuWaterDelivery.Api.Helpers;
using Microsoft.AspNetCore.Http;

namespace GiaPhuWaterDelivery.Api.Tests.Helpers;

public sealed class CurrentUserTests
{
    [Fact]
    public void Reads_identifier_and_role_from_authenticated_principal()
    {
        var currentUser = CreateCurrentUser(
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim(ClaimTypes.Role, "Warehouse"));

        Assert.Equal(42, currentUser.Id);
        Assert.Equal("Warehouse", currentUser.Role);
    }

    [Fact]
    public void Missing_http_context_is_unauthorized()
    {
        var currentUser = new CurrentUser(new HttpContextAccessor());

        var exception = Assert.Throws<BusinessException>(() => currentUser.Id);

        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-number")]
    public void Missing_or_invalid_identifier_is_unauthorized(string identifier)
    {
        var claims = string.IsNullOrEmpty(identifier)
            ? Array.Empty<Claim>()
            : [new Claim(ClaimTypes.NameIdentifier, identifier)];
        var currentUser = CreateCurrentUser(claims);

        var exception = Assert.Throws<BusinessException>(() => currentUser.Id);

        Assert.Equal(StatusCodes.Status401Unauthorized, exception.StatusCode);
    }

    [Fact]
    public void Missing_role_returns_empty_string()
    {
        var currentUser = CreateCurrentUser(
            new Claim(ClaimTypes.NameIdentifier, "1"));

        Assert.Equal(string.Empty, currentUser.Role);
    }

    private static CurrentUser CreateCurrentUser(params Claim[] claims)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };
        return new CurrentUser(new HttpContextAccessor { HttpContext = context });
    }
}
