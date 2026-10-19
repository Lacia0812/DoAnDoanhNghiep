using GiaPhuWaterDelivery.Api.DTOs;
using GiaPhuWaterDelivery.Api.Helpers;
using GiaPhuWaterDelivery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GiaPhuWaterDelivery.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AuthService authService, ICurrentUser currentUser) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    public Task<LoginResponse> Login(LoginRequest request, CancellationToken cancellationToken) =>
        authService.LoginAsync(request, cancellationToken);

    [Authorize]
    [HttpGet("profile")]
    [ProducesResponseType<AuthUserDto>(StatusCodes.Status200OK)]
    public Task<AuthUserDto> Profile(CancellationToken cancellationToken) =>
        authService.ProfileAsync(currentUser.Id, cancellationToken);
}
