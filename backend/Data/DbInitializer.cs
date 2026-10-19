using GiaPhuWaterDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GiaPhuWaterDelivery.Api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var users = await EnsureUsersAsync(db, cancellationToken);
        var categories = await EnsureCategoriesAsync(db, cancellationToken);
        var products = await EnsureProductsAsync(db, categories, users.Admin, cancellationToken);
        var customers = await EnsureCustomersAsync(db, cancellationToken);
        await EnsureDemoOrdersAsync(
            db, users.Sales, users.Warehouse, users.Delivery,
            products, customers, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<(User Admin, User Sales, User Warehouse, User Delivery)> EnsureUsersAsync(
        AppDbContext db, CancellationToken cancellationToken)
    {
        var definitions = new[]
        {
            ("admin", "Quản trị viên", "Admin@123", UserRole.Admin),
            ("sales", "Nhân viên bán hàng", "Sales@123", UserRole.Sales),
            ("warehouse", "Nhân viên kho", "Warehouse@123", UserRole.Warehouse),
            ("delivery", "Nhân viên giao hàng", "Delivery@123", UserRole.Delivery)
        };
        var result = new Dictionary<string, User>();
        foreach (var item in definitions)
        {
            var user = await db.Users.SingleOrDefaultAsync(
                x => x.Username == item.Item1, cancellationToken);
            if (user is null)
            {
                user = new User
                {
                    Username = item.Item1,
                    FullName = item.Item2,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(item.Item3),
                    Role = item.Item4,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                db.Users.Add(user);
            }
            result[item.Item1] = user;
        }
        await db.SaveChangesAsync(cancellationToken);
        return (result["admin"], result["sales"], result["warehouse"], result["delivery"]);
    }

    private static async Task<Dictionary<string, ProductCategory>> EnsureCategoriesAsync(
        AppDbContext db, CancellationToken cancellationToken)
    {
        var definitions = new[]
        {
            ("Bình nước", "Các loại bình nước dung tích lớn"),
            ("Chai nước", "Các loại chai nước uống đóng chai"),
            ("Thùng nước", "Sản phẩm đóng theo thùng")
        };
        var result = new Dictionary<string, ProductCategory>();
        foreach (var (name, description) in definitions)
        {
            var category = await db.ProductCategories.SingleOrDefaultAsync(
                x => x.Name == name, cancellationToken);
            if (category is null)
            {
                category = new ProductCategory { Name = name, Description = description };
                db.ProductCategories.Add(category);
            }
            result[name] = category;
        }
        await db.SaveChangesAsync(cancellationToken);
        return result;
    }

    private static async Task<Dictionary<string, Product>> EnsureProductsAsync(
        AppDbContext db,
        IReadOnlyDictionary<string, ProductCategory> categories,
        User admin,
        CancellationToken cancellationToken)
    {
        var definitions = new[]
        {
            new ProductSeed("SP001", "Bình nước 20 lít", "Bình nước", "20 lít", "Bình", 60000m, 500, 50, true, 50000m),
            new ProductSeed("SP002", "Chai nước 500 ml", "Chai nước", "500 ml", "Chai", 5000m, 800, 100),
            new ProductSeed("SP003", "Chai nước 1,5 lít", "Chai nước", "1,5 lít", "Chai", 12000m, 600, 80),
            new ProductSeed("SP004", "Thùng 24 chai 500 ml", "Thùng nước", "24 x 500 ml", "Thùng", 105000m, 350, 40),
            new ProductSeed("SP005", "Thùng 12 chai 1,5 lít", "Thùng nước", "12 x 1,5 lít", "Thùng", 132000m, 300, 35)
        };
        var result = new Dictionary<string, Product>();
        foreach (var item in definitions)
        {
            var product = await db.Products.SingleOrDefaultAsync(
                x => x.ProductCode == item.Code, cancellationToken);
            if (product is null)
            {
                product = new Product
                {
                    ProductCode = item.Code,
                    ProductName = item.Name,
                    CategoryId = categories[item.Category].Id,
                    Capacity = item.Capacity,
                    Unit = item.Unit,
                    Price = item.Price,
                    StockQuantity = item.InitialStock,
                    MinimumStock = item.MinimumStock,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                db.Products.Add(product);
                await db.SaveChangesAsync(cancellationToken);
                db.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = product.Id,
                    TransactionType = InventoryTransactionType.StockIn,
                    Quantity = item.InitialStock,
                    QuantityBefore = 0,
                    QuantityAfter = item.InitialStock,
                    CreatedByUserId = admin.Id,
                    CreatedAt = DateTime.UtcNow,
                    Note = "Dữ liệu tồn kho khởi tạo"
                });
            }
            product.RequiresContainer = item.RequiresContainer;
            product.ContainerDeposit = item.RequiresContainer ? item.ContainerDeposit : 0;
            result[item.Code] = product;
        }
        await db.SaveChangesAsync(cancellationToken);
        return result;
    }

    private static async Task<Dictionary<string, Customer>> EnsureCustomersAsync(
        AppDbContext db, CancellationToken cancellationToken)
    {
        var definitions = new[]
        {
            new CustomerSeed(
                "TDM-KH-001",
                "Viễn thông Bình Dương (VNPT Bình Dương)",
                "02743834835",
                "Số 326 Đại lộ Bình Dương, phường Phú Lợi, TP.HCM",
                CustomerType.Company,
                Source: "https://binhduong.vnpt.vn/service/1220/2/gioi-thieu-chung",
                LegacyCode: "KH001"),
            new CustomerSeed(
                "TDM-KH-002",
                "Cửa hàng MobiFone Bình Dương",
                "0784713030",
                "491 Đại lộ Bình Dương, phường Thủ Dầu Một, TP.HCM",
                CustomerType.Dealer,
                Source: "https://www.mobifone.vn/ho-tro-khach-hang/vi-tri-cua-hang?focus=5402&province=HCM",
                LegacyCode: "KH002"),
            new CustomerSeed(
                "TDM-KH-003",
                "Bệnh viện Đa khoa Medic Bình Dương",
                "02743855997",
                "14A Nguyễn An Ninh, phường Thủ Dầu Một, TP.HCM",
                CustomerType.Company,
                Email: "medicbinhduong777@gmail.com",
                Source: "https://medicbinhduong.vn/lien-he-voi-chung-toi.html",
                LegacyCode: "KH003"),
            new CustomerSeed(
                "TDM-KH-004",
                "Công ty Cổ phần Đầu tư Xây dựng Chánh Phú Hòa",
                "02743897206",
                "Số 7 Ngô Văn Trị, phường Phú Lợi, TP.HCM",
                CustomerType.Company,
                Source: "https://cphaco.vn/lien-he/",
                LegacyCode: "KH004"),
            new CustomerSeed(
                "TDM-KH-005",
                "GoGi House Nguyễn Kim Bình Dương",
                "02747300211",
                "Số 1 Trần Hưng Đạo, phường Thủ Dầu Một, TP.HCM",
                CustomerType.Restaurant,
                Source: "https://offer.ggg.com.vn/gogi-tang-combo/",
                LegacyCode: "KH005"),
            new CustomerSeed(
                "TDM-CTY-001",
                "Công ty Cổ phần Logistics U&I",
                "02743816288",
                "158 Ngô Gia Tự, phường Thủ Dầu Một, TP.HCM",
                CustomerType.Company,
                Source: "https://www.unilogistics.vn/en/contact",
                LegacyCode: "DEMO-CTY-001"),
            new CustomerSeed(
                "TDM-CTY-002",
                "Công ty CP – Tổng Công ty Nước – Môi trường Bình Dương (BIWASE)",
                "02743825172",
                "Số 11 Ngô Văn Trị, phường Phú Lợi, TP.HCM",
                CustomerType.Company,
                Source: "https://biwase.com.vn/khach-hang/tiep-nhan-thong-tin",
                LegacyCode: "DEMO-CTY-002"),
            new CustomerSeed(
                "TDM-CTY-003",
                "Becamex Hotel Thu Dau Mot",
                "02742221333",
                "230 Đại lộ Bình Dương, phường Phú Lợi, TP.HCM",
                CustomerType.Company,
                Source: "https://thudaumot.becamexhotels.com/",
                LegacyCode: "DEMO-CTY-003"),
            new CustomerSeed(
                "TDM-CTY-004",
                "Công ty Cổ phần Thực phẩm Phú Hòa",
                "0908985232",
                "342 Phú Lợi, phường Phú Lợi, TP.HCM",
                CustomerType.Company,
                Source: "https://phuhoafresh.com/lien-he",
                LegacyCode: "DEMO-CTY-004"),
            new CustomerSeed(
                "TDM-CTY-005",
                "Công ty TNHH FIRSTLY AUTO",
                "0328017018",
                "88/5 đường 1 Tháng 12, phường Phú Lợi, TP.HCM",
                CustomerType.Company,
                Source: "https://firstlyauto.com.vn/about",
                LegacyCode: "DEMO-CTY-005"),
            new CustomerSeed(
                "TDM-CTY-006",
                "Công ty Cổ phần Đầu tư Thế Giới Di Động",
                "02838125960",
                "222 Yersin, phường Thủ Dầu Một, TP.HCM",
                CustomerType.Company,
                Source: "https://cdnv2.tgdd.vn/mwgvn/investorrelations/files/posts/2026/3/5689/07/5f/075f10cc6eb4bab12e3bc0f4070ed654.pdf",
                LegacyCode: "DEMO-CTY-006"),
            new CustomerSeed(
                "TDM-CTY-007",
                "Công ty Cổ phần In Tổng hợp Bình Dương",
                "02743831505",
                "Số 1 Huỳnh Văn Nghệ, phường Phú Lợi, TP.HCM",
                CustomerType.Company,
                Source: "https://www.intonghopbd.com.vn/upload/baiviet/1-2345.pdf",
                LegacyCode: "DEMO-CTY-007"),
            new CustomerSeed(
                "TDM-CTY-008",
                "Công ty Cổ phần Nước Thủ Dầu Một",
                "02743842255",
                "Số 11B Ngô Văn Trị, phường Phú Lợi, TP.HCM",
                CustomerType.Company,
                Source: "https://tdmwater.vn/lien-he",
                LegacyCode: "DEMO-CTY-008"),
            new CustomerSeed(
                "TDM-CTY-009",
                "Công ty Cổ phần Bất động sản cho thuê Minh Bảo Tín",
                "0938090579",
                "A3, đường 1A, Khu nhà ở Phú Lợi Chợ Đình, phường Phú Lợi, TP.HCM",
                CustomerType.Company,
                Source: "https://hnx.vn/vi-vn/m-tim-kiem-MBT.html",
                LegacyCode: "DEMO-CTY-009"),
            new CustomerSeed(
                "TDM-CTY-010",
                "Tổng công ty Thương mại Xuất nhập khẩu Thanh Lễ – CTCP",
                "02743829534",
                "63 Yersin, phường Thủ Dầu Một, TP.HCM",
                CustomerType.Company,
                Source: "https://www.hnx.vn/vi-vn/cophieu-etfs/chi-tiet-chung-khoan-uc-tlp.html",
                LegacyCode: "DEMO-CTY-010"),
            new CustomerSeed(
                "TDM-CTY-011",
                "Công ty Cổ phần Xây dựng Tư vấn Đầu tư Bình Dương (Biconsi)",
                "02743822936",
                "Số 02 Trần Văn Ơn, phường Phú Lợi, TP.HCM",
                CustomerType.Company,
                Source: "https://www.biconsi.com.vn/tin-tuc-su-kien/tin-biconsi/cho-thue-van-phong-lam-viec-tai-toa-nha-biconsi-tower-tp-thu-dau-mot-1?pagenumber=6",
                LegacyCode: "DEMO-CTY-011"),
            new CustomerSeed(
                "TDM-CTY-012",
                "Công ty Cổ phần Phát triển Hạ tầng Kỹ thuật (Becamex IJC)",
                "02743848789",
                "230 Đại lộ Bình Dương, phường Phú Lợi, TP.HCM",
                CustomerType.Company,
                Source: "https://becamexijc.com/wp-content/uploads/2026/03/Tai-lieu-DHDCD_TA-1-233.pdf",
                LegacyCode: "DEMO-CTY-012"),
            new CustomerSeed(
                "TDM-CTY-013",
                "The Mira Hotel",
                "02743678888",
                "Số 555B Đại lộ Bình Dương, phường Thủ Dầu Một, TP.HCM",
                CustomerType.Company,
                Email: "sales@themirahotel.com.vn",
                Source: "https://themirahotel.com.vn/en/contact",
                LegacyCode: "DEMO-CTY-013"),
            new CustomerSeed(
                "TDM-CTY-014",
                "Co.opmart Bình Dương 2",
                "02746259779",
                "Đường Phú Lợi, phường Phú Lợi, TP.HCM",
                CustomerType.Company,
                Source: "https://co-opmart.com.vn/he-thong-sieu-thi/co-opmart-binh-duong-2",
                LegacyCode: "DEMO-CTY-014"),
            new CustomerSeed(
                "TDM-CTY-015",
                "Siêu thị Điện máy – Nội thất Chợ Lớn Thủ Dầu Một",
                "02743689198",
                "Số 283 Đại lộ Bình Dương, phường Thủ Dầu Một, TP.HCM",
                CustomerType.Company,
                Email: "dienmaybinhduong@dienmaycholon.com.vn",
                Source: "https://dienmaycholon.com/he-thong-sieu-thi/283-dai-lo-binh-duong-phuong-chanh-nghia-tpthu-dau-mot-binh-duong",
                LegacyCode: "DEMO-CTY-015"),
            new CustomerSeed(
                "TDM-TH-001",
                "Trường Mầm non Hoa Phượng",
                "02743856388",
                "Số 15 đường Lê Thị Trung, Khu 4, phường Phú Lợi, TP.HCM",
                CustomerType.School,
                Email: "mn-hoaphuong@tptdm.edu.vn",
                Source: "https://mnhoaphuong.phuongphuloi.edu.vn/",
                LegacyCode: "DEMO-TH-001"),
            new CustomerSeed(
                "TDM-TH-002",
                "Trường Tiểu học Phú Lợi",
                "02743856242",
                "Số 96 đường Đoàn Thị Liên, Khu 2, phường Phú Lợi, TP.HCM",
                CustomerType.School,
                Source: "https://thphuloi.phuongphuloi.edu.vn/",
                LegacyCode: "DEMO-TH-002"),
            new CustomerSeed(
                "TDM-TH-003",
                "Trường Tiểu học Phú Lợi 2",
                "02742200869",
                "Đường Mỹ Phước – Tân Vạn, khu phố 8, phường Phú Lợi, TP.HCM",
                CustomerType.School,
                Email: "th-phuloi2@tptdm.edu.vn",
                Source: "https://thphuloi2.phuongphuloi.edu.vn/",
                LegacyCode: "DEMO-TH-003"),
            new CustomerSeed(
                "TDM-TH-004",
                "Trường Tiểu học Phú Hòa 1",
                "02743828762",
                "172 Trần Văn Ơn, phường Phú Lợi, TP.HCM",
                CustomerType.School,
                Email: "th-phuhoa1@tptdm.edu.vn",
                Source: "https://thphuhoa1.phuongphuloi.edu.vn/",
                LegacyCode: "DEMO-TH-004"),
            new CustomerSeed(
                "TDM-TH-005",
                "Trường Tiểu học Phú Hòa 2",
                "02743828766",
                "14 Nguyễn Thị Minh Khai, phường Phú Lợi, TP.HCM",
                CustomerType.School,
                Email: "th-phuhoa2@tptdm.edu.vn",
                Source: "https://thphuhoa2.phuongphuloi.edu.vn/contact/Truong-Tieu-hoc-Phu-Hoa-2/",
                LegacyCode: "DEMO-TH-005"),
            new CustomerSeed(
                "TDM-TH-006",
                "Trường Tiểu học Phú Hòa 3",
                "02743877755",
                "Đường Lê Hồng Phong, Khu 8, phường Phú Lợi, TP.HCM",
                CustomerType.School,
                Email: "th-phuhoa3@tptdm.edu.vn",
                Source: "https://thphuhoa3.phuongphuloi.edu.vn/about/Gioi-thieu-chung.html",
                LegacyCode: "DEMO-TH-006"),
            new CustomerSeed(
                "TDM-TH-007",
                "Trường THCS Phú Hòa",
                "02743828768",
                "157 Nguyễn Thái Bình, phường Phú Lợi, TP.HCM",
                CustomerType.School,
                Email: "thcs-phuhoa@tptdm.edu.vn",
                Source: "https://thcsphuhoa.phuongphuloi.edu.vn/about/Gioi-thieu-chung.html",
                LegacyCode: "DEMO-TH-007"),
            new CustomerSeed(
                "TDM-TH-008",
                "Trường Đại học Thủ Dầu Một",
                "02743822518",
                "Số 6 Trần Văn Ơn, phường Phú Lợi, TP.HCM",
                CustomerType.School,
                Source: "https://tdmu.edu.vn/don-vi-truc-thuoc/ban-bien-tap-trang-thong-tin-dien-tu/thong-tin-lien-lac/thong-tin-lien-lac-phong-truyen-thong",
                LegacyCode: "DEMO-TH-008")
        };
        var result = new Dictionary<string, Customer>();
        foreach (var item in definitions)
        {
            var targetCustomer = await db.Customers.SingleOrDefaultAsync(
                x => x.CustomerCode == item.Code, cancellationToken);
            Customer? legacyCustomer = null;
            if (item.LegacyCode is not null)
            {
                legacyCustomer = await db.Customers.SingleOrDefaultAsync(
                    x => x.CustomerCode == item.LegacyCode, cancellationToken);
            }
            if (targetCustomer is not null && legacyCustomer is not null
                && targetCustomer.Id != legacyCustomer.Id)
            {
                throw new InvalidOperationException(
                    $"Không thể chuyển mã khách hàng {item.LegacyCode} sang {item.Code}: "
                    + $"cả hai mã đang tồn tại với Id khác nhau "
                    + $"({legacyCustomer.Id} và {targetCustomer.Id}).");
            }

            var customer = targetCustomer ?? legacyCustomer;
            if (customer is null)
            {
                customer = new Customer
                {
                    CustomerCode = item.Code,
                    CustomerName = item.Name,
                    Phone = item.Phone,
                    Email = item.Email,
                    Address = item.Address,
                    CustomerType = item.Type,
                    Note = item.Note,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                db.Customers.Add(customer);
            }
            else
            {
                customer.CustomerCode = item.Code;
                customer.CustomerName = item.Name;
                customer.Phone = item.Phone;
                customer.Email = item.Email;
                customer.Address = item.Address;
                customer.CustomerType = item.Type;
                customer.Note = item.Note;
                customer.IsActive = true;
            }
            result[item.Code] = customer;
        }
        await db.SaveChangesAsync(cancellationToken);
        return result;
    }

    private static async Task EnsureDemoOrdersAsync(
        AppDbContext db,
        User sales,
        User warehouse,
        User deliveryUser,
        IReadOnlyDictionary<string, Product> products,
        IReadOnlyDictionary<string, Customer> customers,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var definitions = new List<OrderSeed>
        {
            new("DEMO-NEW", OrderStatus.New, "TDM-KH-001", "SP001", 2, now.AddDays(-1)),
            new("DEMO-CONFIRMED", OrderStatus.Confirmed, "TDM-KH-002", "SP002", 10, now.AddDays(-2)),
            new("DEMO-PREPARING", OrderStatus.Preparing, "TDM-KH-003", "SP003", 5, now.AddDays(-3)),
            new("DEMO-READY", OrderStatus.ReadyForDelivery, "TDM-KH-004", "SP004", 2, now.AddDays(-4)),
            new("DEMO-DELIVERING", OrderStatus.Delivering, "TDM-KH-005", "SP005", 1, now.AddDays(-1)),
            new("DEMO-FAILED", OrderStatus.DeliveryFailed, "TDM-KH-001", "SP001", 1, now.AddDays(-6)),
            new("DEMO-CANCELLED", OrderStatus.Cancelled, "TDM-KH-002", "SP002", 6, now.AddDays(-7)),
            new("DEMO-LOW-STOCK", OrderStatus.Delivered, "TDM-KH-003", "SP002", 720, now.AddDays(-3))
        };
        for (var monthIndex = 0; monthIndex < 12; monthIndex++)
        {
            definitions.Add(new OrderSeed(
                $"DEMO-REV-{monthIndex + 1:00}",
                OrderStatus.Delivered,
                $"TDM-KH-{monthIndex % 5 + 1:000}",
                $"SP{monthIndex % 5 + 1:000}",
                4 + monthIndex,
                now.AddMonths(-monthIndex).AddDays(-Math.Min(10, monthIndex + 1))));
        }

        foreach (var item in definitions)
        {
            var customer = customers[item.CustomerCode];
            var existingOrder = await db.Orders.SingleOrDefaultAsync(
                x => x.OrderCode == item.Code, cancellationToken);
            if (existingOrder is not null)
            {
                var legacyAddress = GetLegacyCustomerAddress(item.CustomerCode);
                if (existingOrder.CustomerId == customer.Id
                    && legacyAddress is not null
                    && string.Equals(
                        existingOrder.DeliveryAddress, legacyAddress, StringComparison.Ordinal))
                {
                    existingOrder.DeliveryAddress = customer.Address;
                }
                if (existingOrder.ProductAmount == 0)
                    existingOrder.ProductAmount = existingOrder.TotalAmount;
                if (existingOrder.PaymentStatus == PaymentStatus.Paid
                    && existingOrder.PaidAmount == 0)
                    existingOrder.PaidAmount = existingOrder.TotalAmount;
                continue;
            }

            var product = products[item.ProductCode];
            var total = product.Price * item.Quantity;
            var order = new Order
            {
                OrderCode = item.Code,
                CustomerId = customer.Id,
                CreatedByUserId = sales.Id,
                OrderDate = item.Date,
                ExpectedDeliveryDate = item.Date.AddDays(1),
                DeliveryAddress = customer.Address,
                TotalAmount = total,
                ProductAmount = total,
                DepositAmount = 0,
                PaidAmount = item.Status == OrderStatus.Delivered ? total : 0,
                PaymentMethod = item.Status == OrderStatus.Delivered
                    ? PaymentMethod.BankTransfer
                    : PaymentMethod.CashOnDelivery,
                PaymentStatus = item.Status == OrderStatus.Delivered
                    ? PaymentStatus.Paid
                    : PaymentStatus.Unpaid,
                OrderStatus = item.Status,
                Note = "Dữ liệu minh họa",
                CreatedAt = item.Date,
                Details =
                [
                    new OrderDetail
                    {
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = total
                    }
                ]
            };
            if (item.Status is OrderStatus.ReadyForDelivery or OrderStatus.Delivering
                or OrderStatus.Delivered or OrderStatus.DeliveryFailed)
            {
                order.DeliveryUserId = deliveryUser.Id;
                order.Delivery = CreateDelivery(item, deliveryUser.Id);
            }
            db.Orders.Add(order);
            await db.SaveChangesAsync(cancellationToken);

            if (item.Status is OrderStatus.Preparing or OrderStatus.ReadyForDelivery
                or OrderStatus.Delivering or OrderStatus.Delivered or OrderStatus.DeliveryFailed)
            {
                var before = product.StockQuantity;
                product.StockQuantity -= item.Quantity;
                db.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = product.Id,
                    OrderId = order.Id,
                    TransactionType = InventoryTransactionType.StockOut,
                    Quantity = item.Quantity,
                    QuantityBefore = before,
                    QuantityAfter = product.StockQuantity,
                    CreatedByUserId = warehouse.Id,
                    CreatedAt = item.Date,
                    Note = $"Dữ liệu xuất kho cho {item.Code}"
                });
                if (item.Status == OrderStatus.DeliveryFailed)
                {
                    var failedBefore = product.StockQuantity;
                    product.StockQuantity += item.Quantity;
                    db.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = product.Id,
                        OrderId = order.Id,
                        TransactionType = InventoryTransactionType.StockIn,
                        Quantity = item.Quantity,
                        QuantityBefore = failedBefore,
                        QuantityAfter = product.StockQuantity,
                        CreatedByUserId = deliveryUser.Id,
                        CreatedAt = item.Date.AddHours(3),
                        Note = $"Hoàn kho do giao thất bại đơn {item.Code}"
                    });
                }
                await db.SaveChangesAsync(cancellationToken);
            }
        }
        await EnsureFinanceDemoDataAsync(db, sales, deliveryUser, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureFinanceDemoDataAsync(
        AppDbContext db,
        User sales,
        User deliveryUser,
        CancellationToken cancellationToken)
    {
        const string orderCode = "DEMO-REV-01";
        const string paymentNote = "Dữ liệu thanh toán một phần minh họa";
        var order = await db.Orders
            .Include(x => x.Customer)
            .Include(x => x.Details).ThenInclude(x => x.Product)
            .Include(x => x.Payments)
            .AsSplitQuery()
            .SingleAsync(x => x.OrderCode == orderCode, cancellationToken);

        foreach (var detail in order.Details.Where(x => x.Product.RequiresContainer))
        {
            var hasTransaction = await db.ContainerTransactions.AnyAsync(
                x => x.OrderId == order.Id && x.ProductId == detail.ProductId,
                cancellationToken);
            if (hasTransaction)
                continue;

            var returned = Math.Max(0, detail.Quantity - 1);
            var balanceBefore = order.Customer.ContainerBalance;
            order.Customer.ContainerBalance += detail.Quantity - returned;
            detail.ReturnedContainerQuantity = returned;
            detail.ContainerDepositAmount =
                (detail.Quantity - returned) * detail.Product.ContainerDeposit;
            db.ContainerTransactions.Add(new ContainerTransaction
            {
                CustomerId = order.CustomerId,
                ProductId = detail.ProductId,
                OrderId = order.Id,
                DeliveredQuantity = detail.Quantity,
                ReturnedQuantity = returned,
                BalanceBefore = balanceBefore,
                BalanceAfter = order.Customer.ContainerBalance,
                CreatedByUserId = deliveryUser.Id,
                CreatedAt = order.Delivery?.CompletedAt ?? order.OrderDate.AddHours(3),
                Note = $"Dữ liệu giao và thu hồi vỏ cho {orderCode}"
            });
        }

        order.ProductAmount = order.Details.Sum(x => x.TotalPrice);
        order.DepositAmount = order.Details.Sum(x => x.ContainerDepositAmount);
        order.TotalAmount = order.ProductAmount + order.DepositAmount;

        if (order.Payments.All(x => x.Note != paymentNote))
        {
            var paidAmount = decimal.Round(order.TotalAmount / 2, 2);
            order.PaidAmount = paidAmount;
            order.PaymentStatus = PaymentStatus.Partial;
            db.Payments.Add(new Payment
            {
                OrderId = order.Id,
                Amount = paidAmount,
                PaymentMethod = PaymentMethod.BankTransfer,
                PaidAt = order.Delivery?.CompletedAt ?? order.OrderDate.AddHours(3),
                RecordedByUserId = sales.Id,
                Note = paymentNote
            });
        }
    }

    private static string? GetLegacyCustomerAddress(string customerCode) =>
        customerCode switch
        {
            "TDM-KH-001" => "12 Nguyễn Huệ, Quận 1, TP.HCM",
            "TDM-KH-002" => "25 Lê Lợi, TP. Thủ Đức, TP.HCM",
            "TDM-KH-003" => "80 Cách Mạng Tháng 8, Quận 3, TP.HCM",
            "TDM-KH-004" => "10 Phan Xích Long, Phú Nhuận, TP.HCM",
            "TDM-KH-005" => "55 Nguyễn Trãi, Quận 5, TP.HCM",
            _ => null
        };

    private static Delivery CreateDelivery(OrderSeed item, int deliveryUserId)
    {
        var status = item.Status switch
        {
            OrderStatus.ReadyForDelivery => DeliveryStatus.Assigned,
            OrderStatus.Delivering => DeliveryStatus.Delivering,
            OrderStatus.Delivered => DeliveryStatus.Completed,
            OrderStatus.DeliveryFailed => DeliveryStatus.Failed,
            _ => DeliveryStatus.Pending
        };
        return new Delivery
        {
            DeliveryUserId = deliveryUserId,
            AssignedAt = item.Date.AddHours(1),
            StartedAt = status is DeliveryStatus.Delivering or DeliveryStatus.Completed or DeliveryStatus.Failed
                ? item.Date.AddHours(2)
                : null,
            CompletedAt = status is DeliveryStatus.Completed or DeliveryStatus.Failed
                ? item.Date.AddHours(3)
                : null,
            Status = status,
            FailureReason = status == DeliveryStatus.Failed
                ? "Khách hàng không có mặt"
                : null,
            Note = "Dữ liệu giao hàng minh họa"
        };
    }

    private sealed record ProductSeed(
        string Code, string Name, string Category, string Capacity,
        string Unit, decimal Price, int InitialStock, int MinimumStock,
        bool RequiresContainer = false, decimal ContainerDeposit = 0);
    private sealed record CustomerSeed(
        string Code, string Name, string Phone, string Address, CustomerType Type,
        string? Email = null, string? Source = null, string? LegacyCode = null)
    {
        public string? Note => Source is null
            ? null
            : $"Thông tin liên hệ công khai, xác minh ngày 27/07/2026. Nguồn: {Source}";
    }
    private sealed record OrderSeed(
        string Code, OrderStatus Status, string CustomerCode,
        string ProductCode, int Quantity, DateTime Date);
}
