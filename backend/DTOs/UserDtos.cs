using System.ComponentModel.DataAnnotations;
using GiaPhuWaterDelivery.Api.Models;

namespace GiaPhuWaterDelivery.Api.DTOs;

public sealed record UserDto(
    int Id,
    string FullName,
    string Username,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt);

public sealed class CreateUserRequest
{
    [Required, MaxLength(150)] public string FullName { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string Username { get; set; } = string.Empty;
    [Required, MinLength(6), MaxLength(100)] public string Password { get; set; } = string.Empty;
    [Required] public UserRole? Role { get; set; }
}

public sealed class UpdateUserRequest
{
    [Required, MaxLength(150)] public string FullName { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string Username { get; set; } = string.Empty;
    [Required] public UserRole? Role { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ChangePasswordRequest
{
    [Required, MinLength(6), MaxLength(100)] public string NewPassword { get; set; } = string.Empty;
}
