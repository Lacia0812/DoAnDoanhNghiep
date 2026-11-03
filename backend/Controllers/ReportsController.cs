using ClosedXML.Excel;
using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.Helpers;
using GiaPhuWaterDelivery.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GiaPhuWaterDelivery.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin")]
public sealed class ReportsController(AppDbContext db) : ControllerBase
{
    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue(DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        var range = NormalizeRange(from, to);
        var rows = await db.Orders.AsNoTracking()
            .Where(x => x.OrderStatus == OrderStatus.Delivered && x.Delivery != null
                && x.Delivery.CompletedAt >= range.FromUtc && x.Delivery.CompletedAt < range.ToUtcExclusive)
            .OrderBy(x => x.Delivery!.CompletedAt)
            .Select(x => new { x.OrderCode, x.Customer.CustomerName, CompletedAt = x.Delivery!.CompletedAt!.Value,
                x.ProductAmount, x.DepositAmount, x.TotalAmount, x.PaidAmount, x.PaymentStatus })
            .ToListAsync(cancellationToken);
        using var workbook = NewWorkbook("Doanh thu", range);
        var sheet = workbook.Worksheet(1);
        WriteHeaders(sheet, 4, "STT", "Mã đơn", "Khách hàng", "Ngày giao", "Tiền hàng", "Tiền cọc", "Tổng tiền", "Đã thu", "Thanh toán");
        var line = 5;
        foreach (var (row, index) in rows.Select((value, index) => (value, index)))
        {
            sheet.Cell(line, 1).Value = index + 1;
            sheet.Cell(line, 2).Value = row.OrderCode;
            sheet.Cell(line, 3).Value = row.CustomerName;
            sheet.Cell(line, 4).Value = VietnamTime.FromUtc(row.CompletedAt);
            sheet.Cell(line, 5).Value = row.ProductAmount;
            sheet.Cell(line, 6).Value = row.DepositAmount;
            sheet.Cell(line, 7).Value = row.TotalAmount;
            sheet.Cell(line, 8).Value = row.PaidAmount;
            sheet.Cell(line, 9).Value = row.PaymentStatus.ToString();
            line++;
        }
        if (rows.Count > 0)
        {
            sheet.Cell(line, 6).Value = "TỔNG";
            sheet.Cell(line, 6).Style.Font.Bold = true;
            sheet.Cell(line, 7).FormulaA1 = $"SUM(G5:G{line - 1})";
            sheet.Cell(line, 8).FormulaA1 = $"SUM(H5:H{line - 1})";
            sheet.Range(5, 5, line, 8).Style.NumberFormat.Format = "#,##0 [$₫-vi-VN]";
        }
        return Excel(workbook, $"BaoCao-DoanhThu-{range.FromLocal:yyyyMMdd}-{range.ToLocal:yyyyMMdd}.xlsx");
    }

    [HttpGet("debts")]
    public async Task<IActionResult> Debts(CancellationToken cancellationToken)
    {
        var rows = await db.Orders.AsNoTracking()
            .Where(x => x.OrderStatus == OrderStatus.Delivered && x.PaidAmount < x.TotalAmount)
            .OrderBy(x => x.Customer.CustomerName).ThenBy(x => x.OrderCode)
            .Select(x => new { x.OrderCode, x.Customer.CustomerCode, x.Customer.CustomerName,
                x.Customer.Phone, x.TotalAmount, x.PaidAmount, Remaining = x.TotalAmount - x.PaidAmount,
                CompletedAt = x.Delivery != null ? x.Delivery.CompletedAt : null })
            .ToListAsync(cancellationToken);
        using var workbook = NewWorkbook("Công nợ", null);
        var sheet = workbook.Worksheet(1);
        WriteHeaders(sheet, 4, "STT", "Mã đơn", "Mã KH", "Khách hàng", "Điện thoại", "Ngày giao", "Tổng tiền", "Đã thu", "Còn nợ");
        var line = 5;
        foreach (var (row, index) in rows.Select((value, index) => (value, index)))
        {
            sheet.Cell(line, 1).Value = index + 1;
            sheet.Cell(line, 2).Value = row.OrderCode;
            sheet.Cell(line, 3).Value = row.CustomerCode;
            sheet.Cell(line, 4).Value = row.CustomerName;
            sheet.Cell(line, 5).Value = row.Phone;
            if (row.CompletedAt.HasValue) sheet.Cell(line, 6).Value = VietnamTime.FromUtc(row.CompletedAt.Value);
            sheet.Cell(line, 7).Value = row.TotalAmount;
            sheet.Cell(line, 8).Value = row.PaidAmount;
            sheet.Cell(line, 9).Value = row.Remaining;
            line++;
        }
        if (rows.Count > 0) sheet.Range(5, 7, line - 1, 9).Style.NumberFormat.Format = "#,##0 [$₫-vi-VN]";
        return Excel(workbook, $"BaoCao-CongNo-{VietnamTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> Inventory(CancellationToken cancellationToken)
    {
        var rows = await db.Products.AsNoTracking().Include(x => x.Category)
            .OrderBy(x => x.ProductName).ToListAsync(cancellationToken);
        using var workbook = NewWorkbook("Tồn kho", null);
        var sheet = workbook.Worksheet(1);
        WriteHeaders(sheet, 4, "STT", "Mã SP", "Sản phẩm", "Danh mục", "Quy cách", "Tồn hiện tại", "Tồn tối thiểu", "Cảnh báo", "Giá bán");
        var line = 5;
        foreach (var (row, index) in rows.Select((value, index) => (value, index)))
        {
            sheet.Cell(line, 1).Value = index + 1;
            sheet.Cell(line, 2).Value = row.ProductCode;
            sheet.Cell(line, 3).Value = row.ProductName;
            sheet.Cell(line, 4).Value = row.Category.Name;
            sheet.Cell(line, 5).Value = $"{row.Capacity} / {row.Unit}";
            sheet.Cell(line, 6).Value = row.StockQuantity;
            sheet.Cell(line, 7).Value = row.MinimumStock;
            sheet.Cell(line, 8).Value = row.StockQuantity < row.MinimumStock ? "Dưới định mức" : "Bình thường";
            sheet.Cell(line, 9).Value = row.Price;
            line++;
        }
        if (rows.Count > 0) sheet.Range(5, 9, line - 1, 9).Style.NumberFormat.Format = "#,##0 [$₫-vi-VN]";
        return Excel(workbook, $"BaoCao-TonKho-{VietnamTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("delivery-performance")]
    public async Task<IActionResult> DeliveryPerformance(DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        var range = NormalizeRange(from, to);
        var deliveries = await db.Deliveries.AsNoTracking().Include(x => x.DeliveryUser)
            .Where(x => x.AssignedAt >= range.FromUtc && x.AssignedAt < range.ToUtcExclusive)
            .ToListAsync(cancellationToken);
        var rows = deliveries.GroupBy(x => new { x.DeliveryUserId, x.DeliveryUser.FullName })
            .Select(group => new
            {
                group.Key.FullName,
                Total = group.Count(),
                Completed = group.Count(x => x.Status == DeliveryStatus.Completed),
                Failed = group.Count(x => x.Status == DeliveryStatus.Failed),
                AverageMinutes = group.Where(x => x.StartedAt.HasValue && x.CompletedAt.HasValue)
                    .Select(x => (x.CompletedAt!.Value - x.StartedAt!.Value).TotalMinutes)
                    .DefaultIfEmpty(0).Average()
            }).OrderByDescending(x => x.Completed).ToArray();
        using var workbook = NewWorkbook("Hiệu suất giao", range);
        var sheet = workbook.Worksheet(1);
        WriteHeaders(sheet, 4, "STT", "Nhân viên", "Được giao", "Thành công", "Thất bại", "Tỷ lệ thành công", "Thời gian TB (phút)");
        var line = 5;
        foreach (var (row, index) in rows.Select((value, index) => (value, index)))
        {
            sheet.Cell(line, 1).Value = index + 1;
            sheet.Cell(line, 2).Value = row.FullName;
            sheet.Cell(line, 3).Value = row.Total;
            sheet.Cell(line, 4).Value = row.Completed;
            sheet.Cell(line, 5).Value = row.Failed;
            sheet.Cell(line, 6).Value = row.Total == 0 ? 0 : row.Completed / (double)row.Total;
            sheet.Cell(line, 7).Value = Math.Round(row.AverageMinutes, 1);
            sheet.Cell(line, 6).Style.NumberFormat.Format = "0.0%";
            line++;
        }
        return Excel(workbook, $"BaoCao-HieuSuatGiao-{range.FromLocal:yyyyMMdd}-{range.ToLocal:yyyyMMdd}.xlsx");
    }

    private static XLWorkbook NewWorkbook(string title, DateRange? range)
    {
        var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(title.Length > 31 ? title[..31] : title);
        sheet.Cell(1, 1).Value = $"GIA PHÚ WATER DELIVERY - {title.ToUpperInvariant()}";
        sheet.Cell(1, 1).Style.Font.SetBold();
        sheet.Cell(1, 1).Style.Font.FontSize = 16;
        sheet.Cell(2, 1).Value = range.HasValue
            ? $"Từ {range.Value.FromLocal:dd/MM/yyyy} đến {range.Value.ToLocal:dd/MM/yyyy}"
            : $"Xuất lúc {VietnamTime.Now:dd/MM/yyyy HH:mm}";
        return workbook;
    }

    private static void WriteHeaders(IXLWorksheet sheet, int row, params string[] headers)
    {
        for (var column = 1; column <= headers.Length; column++) sheet.Cell(row, column).Value = headers[column - 1];
        var range = sheet.Range(row, 1, row, headers.Length);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#0B7285");
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        sheet.SheetView.FreezeRows(row);
    }

    private FileContentResult Excel(XLWorkbook workbook, string fileName)
    {
        foreach (var sheet in workbook.Worksheets)
        {
            sheet.ColumnsUsed().AdjustToContents();
            foreach (var column in sheet.ColumnsUsed()) if (column.Width > 45) column.Width = 45;
            var used = sheet.RangeUsed();
            if (used is not null && used.RangeAddress.LastAddress.RowNumber >= 4)
                sheet.Range(4, 1, used.RangeAddress.LastAddress.RowNumber,
                    used.RangeAddress.LastAddress.ColumnNumber).SetAutoFilter();
        }
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private static DateRange NormalizeRange(DateTime? from, DateTime? to)
    {
        var now = VietnamTime.Now;
        var fromLocal = DateTime.SpecifyKind((from ?? new DateTime(now.Year, now.Month, 1)).Date, DateTimeKind.Unspecified);
        var toLocal = DateTime.SpecifyKind((to ?? now).Date, DateTimeKind.Unspecified);
        if (toLocal < fromLocal) throw new BusinessException(StatusCodes.Status400BadRequest,
            "Ngày kết thúc phải từ ngày bắt đầu trở đi.", "invalid_report_range");
        return new DateRange(fromLocal, toLocal, VietnamTime.ToUtc(fromLocal), VietnamTime.ToUtc(toLocal.AddDays(1)));
    }

    private readonly record struct DateRange(DateTime FromLocal, DateTime ToLocal, DateTime FromUtc, DateTime ToUtcExclusive);
}
