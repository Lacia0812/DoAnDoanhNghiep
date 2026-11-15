using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.Helpers;
using GiaPhuWaterDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace GiaPhuWaterDelivery.Api.Services;

public sealed class InvoiceService(AppDbContext db)
{
    private const string CompanyName = "CÔNG TY TNHH SẢN XUẤT NƯỚC UỐNG TINH KHIẾT GIA PHÚ";
    private const string BrandName = "GIA PHÚ WATER DELIVERY";
    private const string PrimaryColor = "#0B7285";
    private const string LightPrimaryColor = "#E6F5F7";

    public async Task<(byte[] Content, string FileName)> GenerateAsync(
        int orderId, CancellationToken cancellationToken)
    {
        var order = await db.Orders.AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.CreatedByUser)
            .Include(x => x.Details).ThenInclude(x => x.Product)
            .Include(x => x.Payments).ThenInclude(x => x.RecordedByUser)
            .Include(x => x.Delivery).ThenInclude(x => x!.DeliveryUser)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new BusinessException(StatusCodes.Status404NotFound,
                "Không tìm thấy đơn hàng.", "order_not_found");

        var remainingAmount = Math.Max(0, order.TotalAmount - order.PaidAmount);
        var issuedAt = VietnamTime.Now;

        var bytes = Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(9.5f).FontColor(Colors.Grey.Darken3));

                page.Header().Element(container => BuildHeader(container, order, issuedAt));
                page.Content().PaddingVertical(14).Column(column =>
                {
                    column.Spacing(12);
                    column.Item().Element(container => BuildCustomerInformation(container, order));
                    column.Item().Element(container => BuildProductTable(container, order));
                    column.Item().Row(row =>
                    {
                        row.RelativeItem(5).Element(container => BuildOperationalInformation(container, order));
                        row.RelativeItem(4).Element(container => BuildTotals(container, order, remainingAmount));
                    });
                    column.Item().Element(container => BuildPaymentHistory(container, order));

                    if (!string.IsNullOrWhiteSpace(order.Note))
                    {
                        column.Item().Background(Colors.Grey.Lighten4).Border(1)
                            .BorderColor(Colors.Grey.Lighten2).Padding(8).Column(note =>
                            {
                                note.Item().Text("GHI CHÚ").Bold().FontColor(PrimaryColor);
                                note.Item().PaddingTop(3).Text(order.Note);
                            });
                    }

                    column.Item().PaddingTop(8).ShowEntire().Element(BuildSignatures);
                    column.Item().AlignCenter().Text(
                            "Phiếu được lập từ hệ thống quản lý Gia Phú và không thay thế hóa đơn giá trị gia tăng.")
                        .FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                });

                page.Footer().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Text("Cảm ơn quý khách đã sử dụng dịch vụ Gia Phú.")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken1));
                        text.Span("Trang ");
                        text.CurrentPageNumber();
                        text.Span("/");
                        text.TotalPages();
                    });
                });
            });
        }).GeneratePdf();

        return (bytes, $"HoaDon-{order.OrderCode}.pdf");
    }

    private static void BuildHeader(IContainer container, Order order, DateTime issuedAt)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem(6).Column(company =>
                {
                    company.Item().Text(BrandName).FontSize(18).Bold().FontColor(PrimaryColor);
                    company.Item().Text(CompanyName).FontSize(9).SemiBold();
                    company.Item().Text("Nước uống tinh khiết • Giao nước tận nơi")
                        .FontSize(8.5f).FontColor(Colors.Grey.Darken1);
                });
                row.RelativeItem(4).AlignRight().Column(invoice =>
                {
                    invoice.Item().Text("HÓA ĐƠN BÁN HÀNG").FontSize(16).Bold().FontColor(PrimaryColor);
                    invoice.Item().Text("KIÊM PHIẾU GIAO NHẬN").FontSize(9).SemiBold();
                    invoice.Item().PaddingTop(4).Text($"Số: {order.OrderCode}").Bold();
                    invoice.Item().Text($"Ngày lập: {issuedAt:dd/MM/yyyy HH:mm}").FontSize(8.5f);
                });
            });
            column.Item().PaddingTop(8).Height(2).Background(PrimaryColor);
        });
    }

    private static void BuildCustomerInformation(IContainer container, Order order)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Column(column =>
        {
            column.Item().Background(LightPrimaryColor).Padding(7)
                .Text("THÔNG TIN KHÁCH HÀNG VÀ GIAO NHẬN").Bold().FontColor(PrimaryColor);
            column.Item().Padding(8).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(82);
                    columns.RelativeColumn(4);
                    columns.ConstantColumn(80);
                    columns.RelativeColumn(3);
                });

                InformationRow(table, "Mã khách hàng", order.Customer.CustomerCode,
                    "Ngày đặt", VietnamTime.FromUtc(order.OrderDate).ToString("dd/MM/yyyy HH:mm"));
                InformationRow(table, "Khách hàng", order.Customer.CustomerName,
                    "Điện thoại", order.Customer.Phone);
                InformationRow(table, "Địa chỉ giao", order.DeliveryAddress,
                    "Thanh toán", PaymentMethodLabel(order.PaymentMethod));
                InformationRow(table, "Người nhận", order.Delivery?.RecipientName ?? "Chưa ghi nhận",
                    "Trạng thái", $"{OrderStatusLabel(order.OrderStatus)} • {PaymentStatusLabel(order.PaymentStatus)}");
            });
        });
    }

    private static void BuildProductTable(IContainer container, Order order)
    {
        container.Column(column =>
        {
            column.Item().Text("CHI TIẾT HÀNG HÓA").Bold().FontColor(PrimaryColor);
            column.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);
                    columns.ConstantColumn(62);
                    columns.RelativeColumn(4);
                    columns.ConstantColumn(45);
                    columns.ConstantColumn(42);
                    columns.ConstantColumn(76);
                    columns.ConstantColumn(86);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).AlignCenter().Text("STT");
                    header.Cell().Element(HeaderCell).Text("Mã SP");
                    header.Cell().Element(HeaderCell).Text("Tên hàng hóa / Quy cách");
                    header.Cell().Element(HeaderCell).AlignCenter().Text("ĐVT");
                    header.Cell().Element(HeaderCell).AlignRight().Text("SL");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Đơn giá");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Thành tiền");
                });

                var index = 1;
                foreach (var detail in order.Details.OrderBy(x => x.Id))
                {
                    table.Cell().Element(BodyCell).AlignCenter().Text(index++.ToString());
                    table.Cell().Element(BodyCell).Text(detail.Product.ProductCode);
                    table.Cell().Element(BodyCell).Column(product =>
                    {
                        product.Item().Text(detail.Product.ProductName).SemiBold();
                        product.Item().Text(detail.Product.Capacity).FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                    table.Cell().Element(BodyCell).AlignCenter().Text(detail.Product.Unit);
                    table.Cell().Element(BodyCell).AlignRight().Text(detail.Quantity.ToString("N0"));
                    table.Cell().Element(BodyCell).AlignRight().Text(Money(detail.UnitPrice));
                    table.Cell().Element(BodyCell).AlignRight().Text(Money(detail.TotalPrice)).SemiBold();
                }
            });
        });
    }

    private static void BuildOperationalInformation(IContainer container, Order order)
    {
        container.PaddingRight(10).Column(column =>
        {
            column.Spacing(4);
            column.Item().Text("THÔNG TIN ĐỐI SOÁT").Bold().FontColor(PrimaryColor);
            column.Item().Text($"Nhân viên lập đơn: {order.CreatedByUser.FullName}");
            column.Item().Text($"Nhân viên giao: {order.Delivery?.DeliveryUser.FullName ?? "Chưa phân công"}");

            if (order.Delivery?.CompletedAt is not null)
                column.Item().Text($"Hoàn tất giao: {VietnamTime.FromUtc(order.Delivery.CompletedAt.Value):dd/MM/yyyy HH:mm}");

            var containerLines = order.Details.Where(x => x.Product.RequiresContainer).ToArray();
            if (containerLines.Length == 0) return;

            column.Item().PaddingTop(4).Text("ĐỐI SOÁT VỎ BÌNH").Bold().FontColor(PrimaryColor);
            foreach (var detail in containerLines)
            {
                column.Item().Text(
                    $"• {detail.Product.ProductName}: giao {detail.Quantity:N0}, thu {detail.ReturnedContainerQuantity:N0}, " +
                    $"chênh lệch {detail.Quantity - detail.ReturnedContainerQuantity:N0}");
            }
        });
    }

    private static void BuildTotals(IContainer container, Order order, decimal remainingAmount)
    {
        container.Background(Colors.Grey.Lighten4).Border(1).BorderColor(Colors.Grey.Lighten2)
            .Padding(9).Column(total =>
            {
                total.Spacing(5);
                TotalRow(total, "Tiền hàng", order.ProductAmount);
                TotalRow(total, "Tiền cọc vỏ bình", order.DepositAmount);
                total.Item().PaddingVertical(3).BorderTop(1).BorderColor(Colors.Grey.Lighten2);
                TotalRow(total, "TỔNG CỘNG", order.TotalAmount, true, PrimaryColor);
                TotalRow(total, "Đã thanh toán", order.PaidAmount);
                TotalRow(total, "CÒN PHẢI THU", remainingAmount, true,
                    remainingAmount > 0 ? Colors.Red.Darken1 : Colors.Green.Darken1);
                total.Item().PaddingTop(4).Text($"Bằng chữ: {MoneyInWords(order.TotalAmount)}")
                    .FontSize(8).Italic();
            });
    }

    private static void BuildPaymentHistory(IContainer container, Order order)
    {
        container.Column(column =>
        {
            column.Item().Text("LỊCH SỬ THANH TOÁN").Bold().FontColor(PrimaryColor);
            if (order.Payments.Count == 0)
            {
                column.Item().PaddingTop(4).Text("Chưa phát sinh giao dịch thanh toán.")
                    .Italic().FontColor(Colors.Grey.Darken1);
                return;
            }

            column.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(32);
                    columns.ConstantColumn(95);
                    columns.RelativeColumn(2);
                    columns.ConstantColumn(90);
                    columns.RelativeColumn(2);
                });
                table.Header(header =>
                {
                    header.Cell().Element(SubHeaderCell).AlignCenter().Text("STT");
                    header.Cell().Element(SubHeaderCell).Text("Thời gian");
                    header.Cell().Element(SubHeaderCell).Text("Hình thức");
                    header.Cell().Element(SubHeaderCell).AlignRight().Text("Số tiền");
                    header.Cell().Element(SubHeaderCell).Text("Người ghi nhận");
                });

                var index = 1;
                foreach (var payment in order.Payments.OrderBy(x => x.PaidAt))
                {
                    table.Cell().Element(CompactCell).AlignCenter().Text(index++.ToString());
                    table.Cell().Element(CompactCell).Text(VietnamTime.FromUtc(payment.PaidAt).ToString("dd/MM/yyyy HH:mm"));
                    table.Cell().Element(CompactCell).Text(PaymentMethodLabel(payment.PaymentMethod));
                    table.Cell().Element(CompactCell).AlignRight().Text(Money(payment.Amount));
                    table.Cell().Element(CompactCell).Text(payment.RecordedByUser.FullName);
                }
            });
        });
    }

    private static void BuildSignatures(IContainer container)
    {
        container.Row(row =>
        {
            SignatureColumn(row.RelativeItem(), "Người lập phiếu", "(Ký, ghi rõ họ tên)");
            SignatureColumn(row.RelativeItem(), "Nhân viên giao", "(Ký, ghi rõ họ tên)");
            SignatureColumn(row.RelativeItem(), "Khách hàng/Người nhận", "(Ký, ghi rõ họ tên)");
        });
    }

    private static void InformationRow(TableDescriptor table, string firstLabel, string firstValue,
        string secondLabel, string secondValue)
    {
        table.Cell().PaddingVertical(3).Text(firstLabel).SemiBold().FontColor(Colors.Grey.Darken1);
        table.Cell().PaddingVertical(3).Text(firstValue);
        table.Cell().PaddingVertical(3).Text(secondLabel).SemiBold().FontColor(Colors.Grey.Darken1);
        table.Cell().PaddingVertical(3).Text(secondValue);
    }

    private static void TotalRow(ColumnDescriptor column, string label, decimal value,
        bool bold = false, string? color = null)
    {
        column.Item().Row(row =>
        {
            var labelText = row.RelativeItem().Text(label);
            var valueText = row.ConstantItem(100).AlignRight().Text(Money(value));
            if (bold)
            {
                labelText.Bold();
                valueText.Bold();
            }
            if (color is not null)
            {
                labelText.FontColor(color);
                valueText.FontColor(color);
            }
        });
    }

    private static void SignatureColumn(IContainer container, string title, string hint)
    {
        container.AlignCenter().Column(column =>
        {
            column.Item().Text(title).Bold();
            column.Item().Text(hint).FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
            column.Item().Height(56);
        });
    }

    private static IContainer HeaderCell(IContainer value) =>
        value.Background(PrimaryColor).PaddingVertical(7).PaddingHorizontal(5)
            .DefaultTextStyle(x => x.Bold().FontColor(Colors.White).FontSize(8.5f));

    private static IContainer SubHeaderCell(IContainer value) =>
        value.Background(LightPrimaryColor).PaddingVertical(5).PaddingHorizontal(4)
            .BorderBottom(1).BorderColor(PrimaryColor)
            .DefaultTextStyle(x => x.SemiBold().FontColor(PrimaryColor).FontSize(8));

    private static IContainer BodyCell(IContainer value) =>
        value.PaddingVertical(7).PaddingHorizontal(4).BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2).DefaultTextStyle(x => x.FontSize(8.5f));

    private static IContainer CompactCell(IContainer value) =>
        value.PaddingVertical(4).PaddingHorizontal(4).BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2).DefaultTextStyle(x => x.FontSize(8));

    private static string Money(decimal value) => $"{value:N0} ₫";

    private static string PaymentMethodLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Tiền mặt",
        PaymentMethod.BankTransfer => "Chuyển khoản",
        PaymentMethod.CashOnDelivery => "Thu khi giao hàng",
        _ => method.ToString()
    };

    private static string PaymentStatusLabel(PaymentStatus status) => status switch
    {
        PaymentStatus.Unpaid => "Chưa thanh toán",
        PaymentStatus.Partial => "Thanh toán một phần",
        PaymentStatus.Paid => "Đã thanh toán",
        _ => status.ToString()
    };

    private static string OrderStatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.New => "Đơn mới",
        OrderStatus.Confirmed => "Đã xác nhận",
        OrderStatus.Preparing => "Đang chuẩn bị",
        OrderStatus.ReadyForDelivery => "Sẵn sàng giao",
        OrderStatus.Delivering => "Đang giao",
        OrderStatus.Delivered => "Đã giao",
        OrderStatus.DeliveryFailed => "Giao thất bại",
        OrderStatus.Cancelled => "Đã hủy",
        _ => status.ToString()
    };

    internal static string MoneyInWords(decimal value)
    {
        var number = (long)Math.Round(value, 0, MidpointRounding.AwayFromZero);
        if (number == 0) return "Không đồng";

        string[] groupNames = ["", "nghìn", "triệu", "tỷ", "nghìn tỷ", "triệu tỷ"];
        var groups = new List<int>();
        for (var current = number; current > 0; current /= 1000)
            groups.Add((int)(current % 1000));

        var parts = new List<string>();
        for (var index = groups.Count - 1; index >= 0; index--)
        {
            if (groups[index] == 0) continue;
            var readFull = parts.Count > 0 && groups[index] < 100;
            var text = ReadThreeDigits(groups[index], readFull);
            if (index < groupNames.Length && groupNames[index].Length > 0)
                text += $" {groupNames[index]}";
            parts.Add(text);
        }

        var result = string.Join(" ", parts).Trim();
        return char.ToUpperInvariant(result[0]) + result[1..] + " đồng chẵn";
    }

    private static string ReadThreeDigits(int number, bool readFull)
    {
        string[] digits = ["không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín"];
        var hundreds = number / 100;
        var tens = number / 10 % 10;
        var ones = number % 10;
        var words = new List<string>();

        if (hundreds > 0 || readFull)
        {
            words.Add(digits[hundreds]);
            words.Add("trăm");
        }

        if (tens > 1)
        {
            words.Add(digits[tens]);
            words.Add("mươi");
        }
        else if (tens == 1)
        {
            words.Add("mười");
        }
        else if (ones > 0 && (hundreds > 0 || readFull))
        {
            words.Add("lẻ");
        }

        if (ones > 0)
        {
            words.Add(ones switch
            {
                1 when tens > 1 => "mốt",
                4 when tens > 1 => "tư",
                5 when tens > 0 => "lăm",
                _ => digits[ones]
            });
        }

        return string.Join(" ", words);
    }
}
