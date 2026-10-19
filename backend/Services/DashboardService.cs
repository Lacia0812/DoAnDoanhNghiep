using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.DTOs;
using GiaPhuWaterDelivery.Api.Helpers;
using GiaPhuWaterDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GiaPhuWaterDelivery.Api.Services;

public sealed class DashboardService(AppDbContext db)
{
    public async Task<DashboardSummaryDto> SummaryAsync(CancellationToken cancellationToken)
    {
        var localNow = VietnamTime.Now;
        var monthStartUtc = VietnamTime.ToUtc(new DateTime(localNow.Year, localNow.Month, 1));
        var nextMonthUtc = VietnamTime.ToUtc(new DateTime(localNow.Year, localNow.Month, 1).AddMonths(1));

        var monthlyRevenue = await db.Deliveries.AsNoTracking()
            .Where(x => x.Status == DeliveryStatus.Completed &&
                        x.CompletedAt >= monthStartUtc && x.CompletedAt < nextMonthUtc)
            .SumAsync(x => (decimal?)x.Order.TotalAmount, cancellationToken) ?? 0m;

        return new DashboardSummaryDto(
            await db.Products.CountAsync(x => x.IsActive, cancellationToken),
            await db.Customers.CountAsync(x => x.IsActive, cancellationToken),
            await db.Orders.CountAsync(cancellationToken),
            await db.Orders.CountAsync(x => x.OrderStatus == OrderStatus.New, cancellationToken),
            await db.Orders.CountAsync(x => x.OrderStatus == OrderStatus.Delivering, cancellationToken),
            await db.Orders.CountAsync(x => x.OrderStatus == OrderStatus.Delivered, cancellationToken),
            monthlyRevenue,
            await db.Products.CountAsync(
                x => x.IsActive && x.StockQuantity < x.MinimumStock, cancellationToken));
    }

    public async Task<IReadOnlyCollection<RevenuePointDto>> RevenueAsync(
        int months, CancellationToken cancellationToken)
    {
        months = Math.Clamp(months, 1, 24);
        var localNow = VietnamTime.Now;
        var firstLocal = new DateTime(localNow.Year, localNow.Month, 1).AddMonths(-(months - 1));
        var fromUtc = VietnamTime.ToUtc(firstLocal);
        var toUtc = VietnamTime.ToUtc(
            new DateTime(localNow.Year, localNow.Month, 1).AddMonths(1));
        var delivered = await db.Deliveries.AsNoTracking()
            .Where(x => x.Status == DeliveryStatus.Completed &&
                        x.CompletedAt >= fromUtc && x.CompletedAt < toUtc)
            .Select(x => new { x.CompletedAt, x.Order.TotalAmount })
            .ToListAsync(cancellationToken);

        var values = new List<RevenuePointDto>(months);
        for (var i = 0; i < months; i++)
        {
            var month = firstLocal.AddMonths(i);
            var amount = delivered.Where(x =>
                {
                    var local = TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(x.CompletedAt!.Value, DateTimeKind.Utc), VietnamTime.Zone);
                    return local.Year == month.Year && local.Month == month.Month;
                })
                .Sum(x => x.TotalAmount);
            values.Add(new RevenuePointDto(
                month.ToString("yyyy-MM"), $"T{month.Month:00}/{month.Year}", amount));
        }
        return values;
    }

    public async Task<IReadOnlyCollection<OrderStatusPointDto>> OrderStatusAsync(
        CancellationToken cancellationToken)
    {
        var counts = await db.Orders.AsNoTracking()
            .GroupBy(x => x.OrderStatus)
            .Select(x => new { Status = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
        return Enum.GetValues<OrderStatus>()
            .Select(x => new OrderStatusPointDto(x, counts.GetValueOrDefault(x)))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<TopProductDto>> TopProductsAsync(
        int take, CancellationToken cancellationToken)
    {
        take = Math.Clamp(take, 1, 20);
        var values = await db.OrderDetails.AsNoTracking()
            .Where(x => x.Order.OrderStatus == OrderStatus.Delivered)
            .GroupBy(x => new { x.ProductId, x.Product.ProductName })
            .Select(x => new
            {
                x.Key.ProductId,
                x.Key.ProductName,
                QuantitySold = x.Sum(y => y.Quantity),
                Revenue = x.Sum(y => y.TotalPrice)
            })
            .OrderByDescending(x => x.QuantitySold)
            .ThenBy(x => x.ProductName)
            .Take(take)
            .ToListAsync(cancellationToken);
        return values.Select(x => new TopProductDto(
            x.ProductId, x.ProductName, x.QuantitySold, x.Revenue)).ToArray();
    }

    public async Task<IReadOnlyCollection<RestockSuggestionDto>> RestockSuggestionsAsync(
        CancellationToken cancellationToken)
    {
        var fromUtc = DateTime.UtcNow.AddDays(-30);
        var products = await db.Products.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.ProductName)
            .Select(x => new
            {
                x.Id,
                x.ProductCode,
                x.ProductName,
                x.StockQuantity,
                x.MinimumStock,
                Sold = x.OrderDetails
                    .Where(d => d.Order.OrderStatus == OrderStatus.Delivered &&
                                d.Order.Delivery != null &&
                                d.Order.Delivery.CompletedAt >= fromUtc)
                    .Sum(d => (int?)d.Quantity) ?? 0
            })
            .ToListAsync(cancellationToken);

        return products.Select(x =>
        {
            var average = x.Sold / 30m;
            decimal? days = average > 0
                ? Math.Round(x.StockQuantity / average, 2)
                : null;
            return new RestockSuggestionDto(
                x.Id, x.ProductCode, x.ProductName, x.StockQuantity, x.MinimumStock,
                Math.Round(average, 2), days, days.HasValue && days.Value <= 7m,
                x.StockQuantity < x.MinimumStock);
        }).ToArray();
    }
}
