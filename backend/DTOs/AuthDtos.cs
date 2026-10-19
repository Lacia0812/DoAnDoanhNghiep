using System.ComponentModel.DataAnnotations;
using GiaPhuWaterDelivery.Api.Models;

namespace GiaPhuWaterDelivery.Api.DTOs;

public sealed class LoginRequest
{
    [Required, MaxLength(50)] public string Username { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Password { get; set; } = string.Empty;
}

public sealed record AuthUserDto(
    int Id,
    string FullName,
    string Username,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt);
public sealed record LoginResponse(string Token, DateTime ExpiresAt, AuthUserDto User);
