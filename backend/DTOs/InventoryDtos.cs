using GiaPhuWaterDelivery.Api.Models;

namespace GiaPhuWaterDelivery.Api.DTOs;

public sealed record InventoryItemDto(
    int ProductId,
    string ProductCode,
    string ProductName,
    string CategoryName,
    string Unit,
    int StockQuantity,
    int MinimumStock,
    bool IsLowStock);

public sealed record InventoryTransactionDto(
    int Id,
    int ProductId,
    string ProductCode,
    string ProductName,
    int? OrderId,
    string? OrderCode,
    InventoryTransactionType TransactionType,
    int Quantity,
    int QuantityBefore,
    int QuantityAfter,
    int CreatedByUserId,
    string CreatedByName,
    DateTime CreatedAt,
    string? Note);
