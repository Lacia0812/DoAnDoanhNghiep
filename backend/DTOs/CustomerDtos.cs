using System.ComponentModel.DataAnnotations;
using GiaPhuWaterDelivery.Api.Models;

namespace GiaPhuWaterDelivery.Api.DTOs;

public sealed record CustomerDto(
    int Id,
    string CustomerCode,
    string CustomerName,
    string Phone,
    string? Email,
    string Address,
    CustomerType CustomerType,
    int ContainerBalance,
    decimal OutstandingDebt,
    string? Note,
    bool IsActive,
    DateTime CreatedAt);

public class CreateCustomerRequest
{
    [Required, MaxLength(30)] public string CustomerCode { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string CustomerName { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Phone { get; set; } = string.Empty;
    [EmailAddress, MaxLength(150)] public string? Email { get; set; }
    [Required, MaxLength(500)] public string Address { get; set; } = string.Empty;
    [Required] public CustomerType? CustomerType { get; set; }
    [MaxLength(1000)] public string? Note { get; set; }
}

public sealed class UpdateCustomerRequest : CreateCustomerRequest
{
    public bool IsActive { get; set; } = true;
}
