using ClosedXML.Excel;
using GiaPhuWaterDelivery.Api.Controllers;
using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiaPhuWaterDelivery.Api.Tests.Controllers;

public sealed class ReportsControllerTests
{
    [Fact]
    public async Task Inventory_export_uses_the_business_report_template()
    {
        await using var db = CreateDb();
        db.ProductCategories.Add(new ProductCategory { Id = 1, Name = "Bình nước" });
        db.Products.Add(new Product
        {
            Id = 1,
            ProductCode = "GP20",
            ProductName = "Bình nước Gia Phú 20L",
            CategoryId = 1,
            Capacity = "20L",
            Unit = "Bình",
            Price = 55_000m,
            StockQuantity = 4,
            MinimumStock = 10,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var action = await new ReportsController(db).Inventory(CancellationToken.None);
        var file = Assert.IsType<FileContentResult>(action);
        using var stream = new MemoryStream(file.FileContents);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);

        Assert.Equal("GIA PHÚ WATER DELIVERY", sheet.Cell(1, 1).GetString());
        Assert.Equal("BÁO CÁO TỒN KHO", sheet.Cell(4, 1).GetString());
        Assert.Equal("SẢN PHẨM", sheet.Cell(8, 1).GetString());
        Assert.Equal("Mã SP", sheet.Cell(11, 2).GetString());
        Assert.Equal("GP20", sheet.Cell(12, 2).GetString());
        Assert.Equal("Dưới định mức", sheet.Cell(12, 8).GetString());
        Assert.Equal(XLPageOrientation.Landscape, sheet.PageSetup.PageOrientation);
        Assert.Contains(sheet.MergedRanges, range => range.Contains(sheet.Cell(1, 1)));
    }

    [Fact]
    public async Task Empty_revenue_export_still_contains_summary_and_no_data_message()
    {
        await using var db = CreateDb();

        var action = await new ReportsController(db).Revenue(
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), CancellationToken.None);
        var file = Assert.IsType<FileContentResult>(action);
        using var stream = new MemoryStream(file.FileContents);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);

        Assert.Equal("BÁO CÁO DOANH THU", sheet.Cell(4, 1).GetString());
        Assert.Equal("ĐƠN ĐÃ GIAO", sheet.Cell(8, 1).GetString());
        Assert.Equal("0 đơn", sheet.Cell(9, 1).GetString());
        Assert.Contains("Không có đơn hàng", sheet.Cell(12, 1).GetString());
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"report-tests-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }
}
