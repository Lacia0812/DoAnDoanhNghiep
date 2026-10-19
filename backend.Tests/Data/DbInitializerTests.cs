using GiaPhuWaterDelivery.Api.Data;
using GiaPhuWaterDelivery.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GiaPhuWaterDelivery.Api.Tests.Data;

public sealed class DbInitializerTests
{
    private const string VerifiedSourceNotePrefix =
        "Thông tin liên hệ công khai, xác minh ngày 27/07/2026. Nguồn:";

    [Fact]
    public async Task SeedAsync_twice_keeps_verified_tdm_customers_unique_and_correct()
    {
        var options = CreateOptions();

        await using var db = new AppDbContext(options);

        await DbInitializer.SeedAsync(db);
        db.ChangeTracker.Clear();
        await DbInitializer.SeedAsync(db);

        var expectedCodes = Enumerable.Range(1, 15)
            .Select(number => $"TDM-CTY-{number:000}")
            .Concat(Enumerable.Range(1, 5)
                .Select(number => $"TDM-KH-{number:000}"))
            .Concat(Enumerable.Range(1, 8)
                .Select(number => $"TDM-TH-{number:000}"))
            .ToArray();
        var legacyCustomerCodes = Enumerable.Range(1, 5)
            .Select(number => $"KH{number:000}")
            .ToHashSet(StringComparer.Ordinal);
        var allCustomers = await db.Customers.AsNoTracking().ToListAsync();
        var verifiedCustomers = allCustomers
            .Where(customer =>
                customer.CustomerCode.StartsWith("TDM-CTY-") ||
                customer.CustomerCode.StartsWith("TDM-KH-") ||
                customer.CustomerCode.StartsWith("TDM-TH-"))
            .OrderBy(customer => customer.CustomerCode)
            .ToArray();

        Assert.Equal(28, allCustomers.Count);
        Assert.Equal(28, verifiedCustomers.Length);
        Assert.Equal(expectedCodes, verifiedCustomers.Select(customer => customer.CustomerCode));
        Assert.Equal(28, verifiedCustomers.Select(customer => customer.CustomerCode).Distinct().Count());
        Assert.Equal(28, verifiedCustomers.Select(customer => customer.Phone).Distinct().Count());
        Assert.DoesNotContain(
            allCustomers,
            customer =>
                customer.CustomerCode.StartsWith("DEMO-") ||
                legacyCustomerCodes.Contains(customer.CustomerCode));
        Assert.DoesNotContain(
            allCustomers,
            customer => (customer.Email ?? string.Empty)
                .Contains("@example.com", StringComparison.OrdinalIgnoreCase));
        Assert.All(verifiedCustomers, customer =>
        {
            Assert.True(customer.IsActive);
            Assert.StartsWith(VerifiedSourceNotePrefix, customer.Note);
        });
        Assert.Null(verifiedCustomers.Single(
            customer => customer.CustomerCode == "TDM-CTY-001").Email);
        Assert.Equal(
            "sales@themirahotel.com.vn",
            verifiedCustomers.Single(
                customer => customer.CustomerCode == "TDM-CTY-013").Email);
        Assert.Equal(
            "mn-hoaphuong@tptdm.edu.vn",
            verifiedCustomers.Single(
                customer => customer.CustomerCode == "TDM-TH-001").Email);

        var companies = verifiedCustomers
            .Where(customer => customer.CustomerCode.StartsWith("TDM-CTY-"))
            .ToArray();
        Assert.Equal(15, companies.Length);
        Assert.All(
            companies,
            customer => Assert.Equal(CustomerType.Company, customer.CustomerType));

        var schools = verifiedCustomers
            .Where(customer => customer.CustomerCode.StartsWith("TDM-TH-"))
            .ToArray();
        Assert.Equal(8, schools.Length);
        Assert.All(
            schools,
            customer => Assert.Equal(CustomerType.School, customer.CustomerType));

        AssertCustomerRecord(
            verifiedCustomers.Single(customer => customer.CustomerCode == "TDM-KH-001"),
            "TDM-KH-001",
            "Viễn thông Bình Dương (VNPT Bình Dương)",
            "02743834835",
            "Số 326 Đại lộ Bình Dương, phường Phú Lợi, TP.HCM",
            CustomerType.Company,
            email: null);
        AssertCustomerRecord(
            verifiedCustomers.Single(customer => customer.CustomerCode == "TDM-KH-002"),
            "TDM-KH-002",
            "Cửa hàng MobiFone Bình Dương",
            "0784713030",
            "491 Đại lộ Bình Dương, phường Thủ Dầu Một, TP.HCM",
            CustomerType.Dealer,
            email: null);
        AssertCustomerRecord(
            verifiedCustomers.Single(customer => customer.CustomerCode == "TDM-KH-003"),
            "TDM-KH-003",
            "Bệnh viện Đa khoa Medic Bình Dương",
            "02743855997",
            "14A Nguyễn An Ninh, phường Thủ Dầu Một, TP.HCM",
            CustomerType.Company,
            "medicbinhduong777@gmail.com");
        AssertCustomerRecord(
            verifiedCustomers.Single(customer => customer.CustomerCode == "TDM-KH-004"),
            "TDM-KH-004",
            "Công ty Cổ phần Đầu tư Xây dựng Chánh Phú Hòa",
            "02743897206",
            "Số 7 Ngô Văn Trị, phường Phú Lợi, TP.HCM",
            CustomerType.Company,
            email: null);
        AssertCustomerRecord(
            verifiedCustomers.Single(customer => customer.CustomerCode == "TDM-KH-005"),
            "TDM-KH-005",
            "GoGi House Nguyễn Kim Bình Dương",
            "02747300211",
            "Số 1 Trần Hưng Đạo, phường Thủ Dầu Một, TP.HCM",
            CustomerType.Restaurant,
            email: null);

        AssertCustomerFields(
            verifiedCustomers.Single(customer => customer.CustomerCode == "TDM-CTY-001"),
            "TDM-CTY-001",
            "Công ty Cổ phần Logistics U&I",
            "02743816288",
            "158 Ngô Gia Tự, phường Thủ Dầu Một, TP.HCM");
        AssertCustomerFields(
            verifiedCustomers.Single(customer => customer.CustomerCode == "TDM-CTY-015"),
            "TDM-CTY-015",
            "Siêu thị Điện máy – Nội thất Chợ Lớn Thủ Dầu Một",
            "02743689198",
            "Số 283 Đại lộ Bình Dương, phường Thủ Dầu Một, TP.HCM");
        AssertCustomerFields(
            verifiedCustomers.Single(customer => customer.CustomerCode == "TDM-TH-001"),
            "TDM-TH-001",
            "Trường Mầm non Hoa Phượng",
            "02743856388",
            "Số 15 đường Lê Thị Trung, Khu 4, phường Phú Lợi, TP.HCM");
        AssertCustomerFields(
            verifiedCustomers.Single(customer => customer.CustomerCode == "TDM-TH-008"),
            "TDM-TH-008",
            "Trường Đại học Thủ Dầu Một",
            "02743822518",
            "Số 6 Trần Văn Ơn, phường Phú Lợi, TP.HCM");
    }

    [Fact]
    public async Task SeedAsync_migrates_legacy_customer_without_changing_id()
    {
        var options = CreateOptions();

        await using var db = new AppDbContext(options);
        var createdAt = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var legacyCustomer = new Customer
        {
            CustomerCode = "DEMO-CTY-001",
            CustomerName = "Tên demo cũ",
            Phone = "0901000006",
            Email = "legacy@example.com",
            Address = "Địa chỉ demo cũ",
            CustomerType = CustomerType.Individual,
            Note = "Dữ liệu demo cũ",
            IsActive = false,
            CreatedAt = createdAt
        };
        db.Customers.Add(legacyCustomer);
        await db.SaveChangesAsync();
        var legacyId = legacyCustomer.Id;

        db.ChangeTracker.Clear();
        await DbInitializer.SeedAsync(db);
        db.ChangeTracker.Clear();

        var migratedCustomer = await db.Customers.AsNoTracking()
            .SingleAsync(customer => customer.CustomerCode == "TDM-CTY-001");

        Assert.Equal(legacyId, migratedCustomer.Id);
        Assert.Equal(createdAt, migratedCustomer.CreatedAt);
        AssertCustomerFields(
            migratedCustomer,
            "TDM-CTY-001",
            "Công ty Cổ phần Logistics U&I",
            "02743816288",
            "158 Ngô Gia Tự, phường Thủ Dầu Một, TP.HCM");
        Assert.Equal(CustomerType.Company, migratedCustomer.CustomerType);
        Assert.True(migratedCustomer.IsActive);
        Assert.StartsWith(VerifiedSourceNotePrefix, migratedCustomer.Note);
        Assert.Equal(28, await db.Customers.CountAsync());
        Assert.False(await db.Customers.AnyAsync(
            customer => customer.CustomerCode.StartsWith("DEMO-")));
    }

    [Fact]
    public async Task SeedAsync_migrates_legacy_base_customer_and_only_updates_seed_order_address()
    {
        var options = CreateOptions();

        await using var db = new AppDbContext(options);
        await DbInitializer.SeedAsync(db);
        db.ChangeTracker.Clear();

        var customer = await db.Customers
            .SingleAsync(item => item.CustomerCode == "TDM-KH-001");
        var seedOrder = await db.Orders
            .SingleAsync(order => order.OrderCode == "DEMO-NEW");
        var product = await db.Products
            .SingleAsync(item => item.ProductCode == "SP001");
        var customerId = customer.Id;
        var seedOrderId = seedOrder.Id;
        const string legacyAddress = "12 Nguyễn Huệ, Quận 1, TP.HCM";
        const string customOrderCode = "USER-LEGACY-ADDRESS";

        Assert.Equal(customerId, seedOrder.CustomerId);

        customer.CustomerCode = "KH001";
        customer.CustomerName = "Khách hàng mẫu cũ";
        customer.Phone = "0901000001";
        customer.Email = "kh001@example.com";
        customer.Address = legacyAddress;
        customer.CustomerType = CustomerType.Individual;
        customer.Note = "Dữ liệu mẫu cũ";
        customer.IsActive = false;
        seedOrder.DeliveryAddress = legacyAddress;

        var customOrder = new Order
        {
            OrderCode = customOrderCode,
            CustomerId = customerId,
            CreatedByUserId = seedOrder.CreatedByUserId,
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(1),
            DeliveryAddress = legacyAddress,
            TotalAmount = product.Price,
            PaymentMethod = PaymentMethod.CashOnDelivery,
            PaymentStatus = PaymentStatus.Unpaid,
            OrderStatus = OrderStatus.New,
            Note = "Đơn người dùng, không thuộc dữ liệu seed",
            CreatedAt = DateTime.UtcNow,
            Details =
            [
                new OrderDetail
                {
                    ProductId = product.Id,
                    Quantity = 1,
                    UnitPrice = product.Price,
                    TotalPrice = product.Price
                }
            ]
        };
        db.Orders.Add(customOrder);
        await db.SaveChangesAsync();
        var customOrderId = customOrder.Id;

        db.ChangeTracker.Clear();
        await DbInitializer.SeedAsync(db);
        db.ChangeTracker.Clear();

        var migratedCustomer = await db.Customers.AsNoTracking()
            .SingleAsync(item => item.CustomerCode == "TDM-KH-001");
        var migratedSeedOrder = await db.Orders.AsNoTracking()
            .SingleAsync(order => order.OrderCode == "DEMO-NEW");
        var unchangedCustomOrder = await db.Orders.AsNoTracking()
            .SingleAsync(order => order.OrderCode == customOrderCode);

        Assert.Equal(customerId, migratedCustomer.Id);
        AssertCustomerRecord(
            migratedCustomer,
            "TDM-KH-001",
            "Viễn thông Bình Dương (VNPT Bình Dương)",
            "02743834835",
            "Số 326 Đại lộ Bình Dương, phường Phú Lợi, TP.HCM",
            CustomerType.Company,
            email: null);
        Assert.True(migratedCustomer.IsActive);
        Assert.StartsWith(VerifiedSourceNotePrefix, migratedCustomer.Note);
        Assert.False(await db.Customers.AnyAsync(
            item => item.CustomerCode == "KH001"));

        Assert.Equal(seedOrderId, migratedSeedOrder.Id);
        Assert.Equal(customerId, migratedSeedOrder.CustomerId);
        Assert.Equal(
            "Số 326 Đại lộ Bình Dương, phường Phú Lợi, TP.HCM",
            migratedSeedOrder.DeliveryAddress);

        Assert.Equal(customOrderId, unchangedCustomOrder.Id);
        Assert.Equal(customerId, unchangedCustomOrder.CustomerId);
        Assert.Equal(legacyAddress, unchangedCustomOrder.DeliveryAddress);
    }

    private static DbContextOptions<AppDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"db-initializer-tests-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings =>
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
    }

    private static void AssertCustomerFields(
        Customer customer,
        string code,
        string name,
        string phone,
        string address)
    {
        Assert.Equal(code, customer.CustomerCode);
        Assert.Equal(name, customer.CustomerName);
        Assert.Equal(phone, customer.Phone);
        Assert.Equal(address, customer.Address);
    }

    private static void AssertCustomerRecord(
        Customer customer,
        string code,
        string name,
        string phone,
        string address,
        CustomerType customerType,
        string? email)
    {
        AssertCustomerFields(customer, code, name, phone, address);
        Assert.Equal(customerType, customer.CustomerType);
        Assert.Equal(email, customer.Email);
    }
}
