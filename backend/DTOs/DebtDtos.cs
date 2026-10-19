using GiaPhuWaterDelivery.Api.Models;

namespace GiaPhuWaterDelivery.Api.DTOs;

public sealed record DebtDto(
    int OrderId,
    string OrderCode,
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    string CustomerPhone,
    DateTime CompletedAt,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    PaymentStatus PaymentStatus);

public sealed record DebtSummaryDto(
    decimal TotalDebt,
    int DebtOrderCount,
    int DebtCustomerCount);

public sealed record ContainerTransactionDto(
    int Id,
    int CustomerId,
    string CustomerName,
    int ProductId,
    string ProductName,
    int OrderId,
    string OrderCode,
    int DeliveredQuantity,
    int ReturnedQuantity,
    int BalanceBefore,
    int BalanceAfter,
    DateTime CreatedAt,
    string CreatedByName,
    string? Note);
