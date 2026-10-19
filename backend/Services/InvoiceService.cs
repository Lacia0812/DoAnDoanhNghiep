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
    public async Task<(byte[] Content, string FileName)> GenerateAsync(
        int orderId, CancellationToken cancellationToken)
    {
        var order = await db.Orders.AsNoTracking()
            .Include(x => x.Customer)
            .Include(x => x.Details).ThenInclude(x => x.Product)
            .Include(x => x.Payments)
            .SingleOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new BusinessException(StatusCodes.Status404NotFound,
                "Không tìm thấy đơn hàng.", "order_not_found");

        var bytes = Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(x => x.FontFamily("Lato").FontSize(10));
                page.Header().Column(column =>
                {
                    column.Item().Text("GIA PHÚ WATER DELIVERY")
                        .FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text("HÓA ĐƠN BÁN HÀNG").FontSize(16).Bold();
                    column.Item().Text($"Mã đơn: {order.OrderCode}");
                });
                page.Content().PaddingVertical(18).Column(column =>
                {
                    column.Spacing(12);
                    column.Item().Text($"Khách hàng: {order.Customer.CustomerName}").Bold();
                    column.Item().Text($"Điện thoại: {order.Customer.Phone}");
                    column.Item().Text($"Địa chỉ giao: {order.DeliveryAddress}");
                    column.Item().Text($"Ngày đặt: {VietnamTime.FromUtc(order.OrderDate):dd/MM/yyyy HH:mm}");

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(32);
                            columns.RelativeColumn(4);
                            columns.ConstantColumn(55);
                            columns.ConstantColumn(85);
                            columns.ConstantColumn(95);
                        });
                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("STT");
                            header.Cell().Element(HeaderCell).Text("Sản phẩm");
                            header.Cell().Element(HeaderCell).AlignRight().Text("SL");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Đơn giá");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Thành tiền");
                        });
                        var index = 1;
                        foreach (var detail in order.Details.OrderBy(x => x.Id))
                        {
                            table.Cell().Element(BodyCell).Text(index++.ToString());
                            table.Cell().Element(BodyCell).Text(detail.Product.ProductName);
                            table.Cell().Element(BodyCell).AlignRight().Text(detail.Quantity.ToString("N0"));
                            table.Cell().Element(BodyCell).AlignRight().Text(Money(detail.UnitPrice));
                            table.Cell().Element(BodyCell).AlignRight().Text(Money(detail.TotalPrice));
                        }
                    });

                    column.Item().AlignRight().Column(total =>
                    {
                        total.Item().Text($"Tiền hàng: {Money(order.ProductAmount)}");
                        total.Item().Text($"Tiền cọc vỏ bình: {Money(order.DepositAmount)}");
                        total.Item().Text($"Tổng cộng: {Money(order.TotalAmount)}").FontSize(13).Bold();
                        total.Item().Text($"Đã thanh toán: {Money(order.PaidAmount)}");
                        total.Item().Text($"Còn phải thu: {Money(order.TotalAmount - order.PaidAmount)}")
                            .Bold().FontColor(Colors.Red.Darken1);
                    });

                    var containerLines = order.Details.Where(x => x.Product.RequiresContainer).ToArray();
                    if (containerLines.Length > 0)
                    {
                        column.Item().Text("Vỏ bình").Bold();
                        foreach (var detail in containerLines)
                            column.Item().Text(
                                $"• {detail.Product.ProductName}: giao {detail.Quantity}, thu {detail.ReturnedContainerQuantity}, cọc {Money(detail.ContainerDepositAmount)}");
                    }
                });
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Cảm ơn quý khách đã sử dụng dịch vụ Gia Phú · Trang ");
                    text.CurrentPageNumber();
                    text.Span("/");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
        return (bytes, $"HoaDon-{order.OrderCode}.pdf");
    }

    private static IContainer HeaderCell(IContainer value) =>
        value.Background(Colors.Grey.Lighten2).Padding(6).BorderBottom(1);
    private static IContainer BodyCell(IContainer value) =>
        value.PaddingVertical(6).PaddingHorizontal(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
    private static string Money(decimal value) => $"{value:N0} ₫";
}
