using System.ComponentModel.DataAnnotations;

namespace GiaPhuWaterDelivery.Api.DTOs;

public sealed record ProductCategoryDto(int Id, string Name, string? Description);

public sealed record ProductDto(
    int Id,
    string ProductCode,
    string ProductName,
    int CategoryId,
    string CategoryName,
    string Capacity,
    string Unit,
    decimal Price,
    bool RequiresContainer,
    decimal ContainerDeposit,
    int StockQuantity,
    int MinimumStock,
    bool IsLowStock,
    bool IsActive,
    DateTime CreatedAt);

public sealed class CreateProductRequest
{
    [Required, MaxLength(30)] public string ProductCode { get; set; } = string.Empty;
    [Required, MaxLength(150)] public string ProductName { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
    [Required, MaxLength(50)] public string Capacity { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string Unit { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "9999999999999999")] public decimal Price { get; set; }
    public bool RequiresContainer { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999")] public decimal ContainerDeposit { get; set; }
    [Range(0, int.MaxValue)] public int InitialStock { get; set; }
    [Range(0, int.MaxValue)] public int MinimumStock { get; set; }
}

public sealed class UpdateProductRequest
{
    [Required, MaxLength(30)] public string ProductCode { get; set; } = string.Empty;
    [Required, MaxLength(150)] public string ProductName { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
    [Required, MaxLength(50)] public string Capacity { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string Unit { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "9999999999999999")] public decimal Price { get; set; }
    public bool RequiresContainer { get; set; }
    [Range(typeof(decimal), "0", "9999999999999999")] public decimal ContainerDeposit { get; set; }
    [Range(0, int.MaxValue)] public int MinimumStock { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class AddStockRequest
{
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
    [MaxLength(1000)] public string? Note { get; set; }
}
