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
    private const int MetricLabelRow = 8;
    private const int MetricValueRow = 9;
    private const int HeaderRow = 11;
    private const int DataStartRow = HeaderRow + 1;
    private const string PrimaryColor = "#0B7285";
    private const string DarkPrimaryColor = "#075360";
    private const string LightPrimaryColor = "#E6F5F7";
    private const string BorderColor = "#B7CDD2";

    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue(DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        var range = NormalizeRange(from, to);
        var rows = await db.Orders.AsNoTracking()
            .Where(x => x.OrderStatus == OrderStatus.Delivered && x.Delivery != null
                && x.Delivery.CompletedAt >= range.FromUtc && x.Delivery.CompletedAt < range.ToUtcExclusive)
            .OrderBy(x => x.Delivery!.CompletedAt)
            .Select(x => new
            {
                x.OrderCode,
                x.Customer.CustomerName,
                CompletedAt = x.Delivery!.CompletedAt!.Value,
                x.ProductAmount,
                x.DepositAmount,
                x.TotalAmount,
                x.PaidAmount,
                x.PaymentStatus
            })
            .ToListAsync(cancellationToken);

        using var workbook = NewWorkbook(
            "BÁO CÁO DOANH THU",
            $"Kỳ báo cáo: từ {range.FromLocal:dd/MM/yyyy} đến {range.ToLocal:dd/MM/yyyy}", 9);
        var sheet = workbook.Worksheet(1);

        WriteMetric(sheet, 1, 2, "ĐƠN ĐÃ GIAO", $"{rows.Count:N0} đơn");
        WriteMetric(sheet, 3, 4, "TỔNG DOANH THU", Money(rows.Sum(x => x.TotalAmount)));
        WriteMetric(sheet, 5, 6, "ĐÃ THU", Money(rows.Sum(x => x.PaidAmount)));
        WriteMetric(sheet, 7, 9, "CÒN PHẢI THU", Money(rows.Sum(x => x.TotalAmount - x.PaidAmount)), "#C92A2A");

        WriteHeaders(sheet, "STT", "Mã đơn", "Khách hàng", "Ngày giao", "Tiền hàng",
            "Tiền cọc", "Tổng tiền", "Đã thu", "Thanh toán");

        var line = DataStartRow;
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
            sheet.Cell(line, 9).Value = PaymentStatusLabel(row.PaymentStatus);
            line++;
        }

        if (rows.Count == 0)
        {
            WriteNoData(sheet, 9, "Không có đơn hàng đã giao trong kỳ báo cáo.");
        }
        else
        {
            sheet.Range(DataStartRow, 4, line - 1, 4).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
            sheet.Range(DataStartRow, 5, line - 1, 8).Style.NumberFormat.Format = "#,##0 [$₫-vi-VN]";
            WriteTotalRow(sheet, line, 9, "TỔNG CỘNG", 6,
                (7, rows.Sum(x => x.TotalAmount)),
                (8, rows.Sum(x => x.PaidAmount)));
        }

        FinishSheet(sheet, 9, rows.Count > 0);
        return Excel(workbook, $"BaoCao-DoanhThu-{range.FromLocal:yyyyMMdd}-{range.ToLocal:yyyyMMdd}.xlsx");
    }

    [HttpGet("debts")]
    public async Task<IActionResult> Debts(CancellationToken cancellationToken)
    {
        var rows = await db.Orders.AsNoTracking()
            .Where(x => x.OrderStatus == OrderStatus.Delivered && x.PaidAmount < x.TotalAmount)
            .OrderBy(x => x.Customer.CustomerName).ThenBy(x => x.OrderCode)
            .Select(x => new
            {
                x.OrderCode,
                x.Customer.CustomerCode,
                x.Customer.CustomerName,
                x.Customer.Phone,
                x.TotalAmount,
                x.PaidAmount,
                Remaining = x.TotalAmount - x.PaidAmount,
                CompletedAt = x.Delivery != null ? x.Delivery.CompletedAt : null
            })
            .ToListAsync(cancellationToken);

        using var workbook = NewWorkbook(
            "BÁO CÁO CÔNG NỢ KHÁCH HÀNG",
            $"Thời điểm đối soát: {VietnamTime.Now:dd/MM/yyyy HH:mm}", 9);
        var sheet = workbook.Worksheet(1);

        WriteMetric(sheet, 1, 3, "KHÁCH HÀNG CÒN NỢ", $"{rows.Select(x => x.CustomerCode).Distinct().Count():N0} khách hàng");
        WriteMetric(sheet, 4, 6, "ĐƠN CÒN NỢ", $"{rows.Count:N0} đơn");
        WriteMetric(sheet, 7, 9, "TỔNG CÔNG NỢ", Money(rows.Sum(x => x.Remaining)), "#C92A2A");

        WriteHeaders(sheet, "STT", "Mã đơn", "Mã KH", "Khách hàng", "Điện thoại",
            "Ngày giao", "Tổng tiền", "Đã thu", "Còn nợ");

        var line = DataStartRow;
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

        if (rows.Count == 0)
        {
            WriteNoData(sheet, 9, "Không có công nợ cần thu tại thời điểm xuất báo cáo.");
        }
        else
        {
            sheet.Range(DataStartRow, 6, line - 1, 6).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
            sheet.Range(DataStartRow, 7, line - 1, 9).Style.NumberFormat.Format = "#,##0 [$₫-vi-VN]";
            WriteTotalRow(sheet, line, 9, "TỔNG CỘNG", 6,
                (7, rows.Sum(x => x.TotalAmount)),
                (8, rows.Sum(x => x.PaidAmount)),
                (9, rows.Sum(x => x.Remaining)));
        }

        FinishSheet(sheet, 9, rows.Count > 0);
        return Excel(workbook, $"BaoCao-CongNo-{VietnamTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> Inventory(CancellationToken cancellationToken)
    {
        var rows = await db.Products.AsNoTracking().Include(x => x.Category)
            .OrderByDescending(x => x.IsActive).ThenBy(x => x.ProductName)
            .ToListAsync(cancellationToken);

        using var workbook = NewWorkbook(
            "BÁO CÁO TỒN KHO",
            $"Thời điểm chốt số liệu: {VietnamTime.Now:dd/MM/yyyy HH:mm}", 10);
        var sheet = workbook.Worksheet(1);

        WriteMetric(sheet, 1, 2, "SẢN PHẨM", $"{rows.Count:N0} mặt hàng");
        WriteMetric(sheet, 3, 5, "TỔNG TỒN", $"{rows.Sum(x => x.StockQuantity):N0} đơn vị");
        WriteMetric(sheet, 6, 8, "DƯỚI ĐỊNH MỨC", $"{rows.Count(x => x.IsActive && x.StockQuantity < x.MinimumStock):N0} mặt hàng", "#C92A2A");
        WriteMetric(sheet, 9, 10, "GIÁ TRỊ TỒN", Money(rows.Sum(x => x.StockQuantity * x.Price)));

        WriteHeaders(sheet, "STT", "Mã SP", "Sản phẩm", "Danh mục", "Quy cách", "Tồn hiện tại",
            "Tồn tối thiểu", "Cảnh báo", "Giá bán", "Trạng thái");

        var line = DataStartRow;
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
            sheet.Cell(line, 10).Value = row.IsActive ? "Đang kinh doanh" : "Ngừng kinh doanh";

            if (row.IsActive && row.StockQuantity < row.MinimumStock)
            {
                sheet.Range(line, 1, line, 10).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF0F0");
                sheet.Cell(line, 8).Style.Font.FontColor = XLColor.FromHtml("#C92A2A");
                sheet.Cell(line, 8).Style.Font.Bold = true;
            }
            line++;
        }

        if (rows.Count == 0)
        {
            WriteNoData(sheet, 10, "Chưa có sản phẩm trong danh mục tồn kho.");
        }
        else
        {
            sheet.Range(DataStartRow, 9, line - 1, 9).Style.NumberFormat.Format = "#,##0 [$₫-vi-VN]";
            WriteTotalRow(sheet, line, 10, "TỔNG TỒN", 5,
                (6, rows.Sum(x => (decimal)x.StockQuantity)));
        }

        FinishSheet(sheet, 10, rows.Count > 0);
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
            }).OrderByDescending(x => x.Completed).ThenBy(x => x.FullName).ToArray();

        using var workbook = NewWorkbook(
            "BÁO CÁO HIỆU SUẤT GIAO HÀNG",
            $"Kỳ báo cáo: từ {range.FromLocal:dd/MM/yyyy} đến {range.ToLocal:dd/MM/yyyy}", 7);
        var sheet = workbook.Worksheet(1);
        var completed = deliveries.Count(x => x.Status == DeliveryStatus.Completed);

        WriteMetric(sheet, 1, 2, "TỔNG LƯỢT GIAO", $"{deliveries.Count:N0} lượt");
        WriteMetric(sheet, 3, 4, "THÀNH CÔNG", $"{completed:N0} lượt", "#2B8A3E");
        WriteMetric(sheet, 5, 6, "THẤT BẠI", $"{deliveries.Count(x => x.Status == DeliveryStatus.Failed):N0} lượt", "#C92A2A");
        WriteMetric(sheet, 7, 7, "TỶ LỆ", deliveries.Count == 0 ? "0,0%" : $"{completed / (double)deliveries.Count:P1}");

        WriteHeaders(sheet, "STT", "Nhân viên", "Được giao", "Thành công", "Thất bại",
            "Tỷ lệ thành công", "Thời gian TB (phút)");

        var line = DataStartRow;
        foreach (var (row, index) in rows.Select((value, index) => (value, index)))
        {
            sheet.Cell(line, 1).Value = index + 1;
            sheet.Cell(line, 2).Value = row.FullName;
            sheet.Cell(line, 3).Value = row.Total;
            sheet.Cell(line, 4).Value = row.Completed;
            sheet.Cell(line, 5).Value = row.Failed;
            sheet.Cell(line, 6).Value = row.Total == 0 ? 0 : row.Completed / (double)row.Total;
            sheet.Cell(line, 7).Value = Math.Round(row.AverageMinutes, 1);
            line++;
        }

        if (rows.Length == 0)
        {
            WriteNoData(sheet, 7, "Không có lượt giao hàng trong kỳ báo cáo.");
        }
        else
        {
            sheet.Range(DataStartRow, 6, line - 1, 6).Style.NumberFormat.Format = "0.0%";
            WriteTotalRow(sheet, line, 7, "TỔNG CỘNG", 2,
                (3, deliveries.Count),
                (4, completed),
                (5, deliveries.Count(x => x.Status == DeliveryStatus.Failed)));
            sheet.Cell(line, 6).Value = deliveries.Count == 0 ? 0 : completed / (double)deliveries.Count;
            sheet.Cell(line, 6).Style.NumberFormat.Format = "0.0%";
        }

        FinishSheet(sheet, 7, rows.Length > 0);
        return Excel(workbook, $"BaoCao-HieuSuatGiao-{range.FromLocal:yyyyMMdd}-{range.ToLocal:yyyyMMdd}.xlsx");
    }

    private static XLWorkbook NewWorkbook(string title, string subtitle, int columnCount)
    {
        var workbook = new XLWorkbook();
        var safeName = title.Replace("BÁO CÁO ", string.Empty);
        var sheet = workbook.Worksheets.Add(safeName.Length > 31 ? safeName[..31] : safeName);
        sheet.ShowGridLines = false;

        sheet.Range(1, 1, 1, columnCount).Merge();
        sheet.Cell(1, 1).Value = "GIA PHÚ WATER DELIVERY";
        sheet.Cell(1, 1).Style
            .Font.SetBold().Font.SetFontSize(18).Font.SetFontColor(XLColor.White)
            .Fill.SetBackgroundColor(XLColor.FromHtml(DarkPrimaryColor))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        sheet.Row(1).Height = 30;

        sheet.Range(2, 1, 2, columnCount).Merge();
        sheet.Cell(2, 1).Value = "CÔNG TY TNHH SẢN XUẤT NƯỚC UỐNG TINH KHIẾT GIA PHÚ";
        sheet.Cell(2, 1).Style.Font.SetBold().Font.SetFontSize(10).Font.SetFontColor(XLColor.FromHtml(DarkPrimaryColor));

        sheet.Range(4, 1, 4, columnCount).Merge();
        sheet.Cell(4, 1).Value = title;
        sheet.Cell(4, 1).Style.Font.SetBold().Font.SetFontSize(17).Font.SetFontColor(XLColor.FromHtml(PrimaryColor));
        sheet.Cell(4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        sheet.Range(5, 1, 5, columnCount).Merge();
        sheet.Cell(5, 1).Value = subtitle;
        sheet.Cell(5, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        sheet.Cell(5, 1).Style.Font.Italic = true;

        sheet.Range(6, 1, 6, columnCount).Merge();
        sheet.Cell(6, 1).Value = $"Xuất từ hệ thống lúc {VietnamTime.Now:dd/MM/yyyy HH:mm}";
        sheet.Cell(6, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        sheet.Cell(6, 1).Style.Font.FontColor = XLColor.Gray;
        sheet.Cell(6, 1).Style.Font.FontSize = 9;

        return workbook;
    }

    private static void WriteMetric(IXLWorksheet sheet, int startColumn, int endColumn,
        string label, string value, string valueColor = PrimaryColor)
    {
        var labelRange = sheet.Range(MetricLabelRow, startColumn, MetricLabelRow, endColumn);
        var valueRange = sheet.Range(MetricValueRow, startColumn, MetricValueRow, endColumn);
        if (startColumn != endColumn)
        {
            labelRange.Merge();
            valueRange.Merge();
        }

        labelRange.FirstCell().Value = label;
        valueRange.FirstCell().Value = value;
        labelRange.Style.Fill.BackgroundColor = XLColor.FromHtml(LightPrimaryColor);
        valueRange.Style.Fill.BackgroundColor = XLColor.FromHtml(LightPrimaryColor);
        labelRange.Style.Font.Bold = true;
        labelRange.Style.Font.FontSize = 8;
        labelRange.Style.Font.FontColor = XLColor.FromHtml(DarkPrimaryColor);
        valueRange.Style.Font.Bold = true;
        valueRange.Style.Font.FontSize = 13;
        valueRange.Style.Font.FontColor = XLColor.FromHtml(valueColor);
        labelRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        valueRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        labelRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        valueRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        labelRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        valueRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        labelRange.Style.Border.OutsideBorderColor = XLColor.FromHtml(BorderColor);
        valueRange.Style.Border.OutsideBorderColor = XLColor.FromHtml(BorderColor);
        sheet.Row(MetricValueRow).Height = 23;
    }

    private static void WriteHeaders(IXLWorksheet sheet, params string[] headers)
    {
        for (var column = 1; column <= headers.Length; column++)
            sheet.Cell(HeaderRow, column).Value = headers[column - 1];

        var range = sheet.Range(HeaderRow, 1, HeaderRow, headers.Length);
        range.Style.Font.Bold = true;
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml(PrimaryColor);
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        range.Style.Alignment.WrapText = true;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.OutsideBorderColor = XLColor.FromHtml(DarkPrimaryColor);
        range.Style.Border.InsideBorderColor = XLColor.FromHtml(DarkPrimaryColor);
        sheet.Row(HeaderRow).Height = 28;
    }

    private static void WriteNoData(IXLWorksheet sheet, int lastColumn, string message)
    {
        var range = sheet.Range(DataStartRow, 1, DataStartRow, lastColumn);
        range.Merge();
        range.FirstCell().Value = message;
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        range.Style.Font.Italic = true;
        range.Style.Font.FontColor = XLColor.Gray;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFB");
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.OutsideBorderColor = XLColor.FromHtml(BorderColor);
        sheet.Row(DataStartRow).Height = 34;
    }

    private static void WriteTotalRow(IXLWorksheet sheet, int row, int lastColumn,
        string label, int labelColumn, params (int Column, decimal Value)[] values)
    {
        sheet.Cell(row, labelColumn).Value = label;
        foreach (var value in values)
        {
            sheet.Cell(row, value.Column).Value = value.Value;
            sheet.Cell(row, value.Column).Style.NumberFormat.Format = "#,##0 [$₫-vi-VN]";
        }

        var range = sheet.Range(row, 1, row, lastColumn);
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml(LightPrimaryColor);
        range.Style.Border.TopBorder = XLBorderStyleValues.Double;
        range.Style.Border.TopBorderColor = XLColor.FromHtml(PrimaryColor);
    }

    private static void FinishSheet(IXLWorksheet sheet, int lastColumn, bool hasData)
    {
        var used = sheet.RangeUsed();
        var lastRow = used?.RangeAddress.LastAddress.RowNumber ?? DataStartRow;

        if (hasData)
        {
            var dataLastRow = lastRow;
            for (var row = DataStartRow; row <= dataLastRow; row++)
            {
                if ((row - DataStartRow) % 2 == 1 && sheet.Row(row).Style.Fill.BackgroundColor == XLColor.NoColor)
                    sheet.Range(row, 1, row, lastColumn).Style.Fill.BackgroundColor = XLColor.FromHtml("#F7FBFC");
            }

            sheet.Range(HeaderRow, 1, lastRow, lastColumn).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            sheet.Range(HeaderRow, 1, lastRow, lastColumn).Style.Border.InsideBorder = XLBorderStyleValues.Hair;
            sheet.Range(HeaderRow, 1, lastRow, lastColumn).Style.Border.OutsideBorderColor = XLColor.FromHtml(BorderColor);
            sheet.Range(HeaderRow, 1, lastRow, lastColumn).Style.Border.InsideBorderColor = XLColor.FromHtml("#DDE8EA");
            sheet.Range(HeaderRow, 1, lastRow - 1, lastColumn).SetAutoFilter();
        }

        sheet.SheetView.FreezeRows(HeaderRow);
        sheet.Columns(1, lastColumn).AdjustToContents(1, lastRow);
        foreach (var column in sheet.Columns(1, lastColumn))
        {
            if (column.Width < 8) column.Width = 8;
            if (column.Width > 38) column.Width = 38;
        }
        sheet.Column(1).Width = 7;

        sheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        sheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
        sheet.PageSetup.FitToPages(1, 0);
        sheet.PageSetup.Margins.Left = 0.25;
        sheet.PageSetup.Margins.Right = 0.25;
        sheet.PageSetup.Margins.Top = 0.4;
        sheet.PageSetup.Margins.Bottom = 0.4;
        sheet.PageSetup.SetRowsToRepeatAtTop(1, HeaderRow);
        sheet.PageSetup.Footer.Center.AddText("Trang &P / &N");
        sheet.PageSetup.Footer.Right.AddText("Gia Phú Water Delivery");
    }

    private FileContentResult Excel(XLWorkbook workbook, string fileName)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private static DateRange NormalizeRange(DateTime? from, DateTime? to)
    {
        var now = VietnamTime.Now;
        var fromLocal = DateTime.SpecifyKind((from ?? new DateTime(now.Year, now.Month, 1)).Date, DateTimeKind.Unspecified);
        var toLocal = DateTime.SpecifyKind((to ?? now).Date, DateTimeKind.Unspecified);
        if (toLocal < fromLocal)
            throw new BusinessException(StatusCodes.Status400BadRequest,
                "Ngày kết thúc phải từ ngày bắt đầu trở đi.", "invalid_report_range");

        return new DateRange(fromLocal, toLocal, VietnamTime.ToUtc(fromLocal), VietnamTime.ToUtc(toLocal.AddDays(1)));
    }

    private static string Money(decimal value) => $"{value:N0} ₫";

    private static string PaymentStatusLabel(PaymentStatus status) => status switch
    {
        PaymentStatus.Unpaid => "Chưa thanh toán",
        PaymentStatus.Partial => "Thanh toán một phần",
        PaymentStatus.Paid => "Đã thanh toán",
        _ => status.ToString()
    };

    private readonly record struct DateRange(
        DateTime FromLocal, DateTime ToLocal, DateTime FromUtc, DateTime ToUtcExclusive);
}
