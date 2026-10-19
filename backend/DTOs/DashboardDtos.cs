using GiaPhuWaterDelivery.Api.Models;

namespace GiaPhuWaterDelivery.Api.DTOs;

public sealed record DashboardSummaryDto(
    int TotalProducts,
    int TotalCustomers,
    int TotalOrders,
    int NewOrders,
    int DeliveringOrders,
    int DeliveredOrders,
    decimal MonthlyRevenue,
    int LowStockProducts);

public sealed record RevenuePointDto(string Month, string Label, decimal Revenue);
public sealed record OrderStatusPointDto(OrderStatus Status, int Count);
public sealed record TopProductDto(int ProductId, string ProductName, int QuantitySold, decimal Revenue);
public sealed record RestockSuggestionDto(
    int ProductId,
    string ProductCode,
    string ProductName,
    int StockQuantity,
    int MinimumStock,
    decimal AverageDailySales,
    decimal? DaysRemaining,
    bool NeedsRestock,
    bool IsBelowMinimum);
