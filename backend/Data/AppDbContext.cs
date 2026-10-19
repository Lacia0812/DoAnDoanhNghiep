using GiaPhuWaterDelivery.Api.Models;
using GiaPhuWaterDelivery.Api.Helpers;
using Microsoft.EntityFrameworkCore;

namespace GiaPhuWaterDelivery.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ContainerTransaction> ContainerTransactions => Set<ContainerTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasIndex(x => x.Username).IsUnique();
        modelBuilder.Entity<ProductCategory>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(x => x.ProductCode).IsUnique();
        modelBuilder.Entity<Customer>().HasIndex(x => x.CustomerCode).IsUnique();
        modelBuilder.Entity<Order>().HasIndex(x => x.OrderCode).IsUnique();
        modelBuilder.Entity<Delivery>().HasIndex(x => x.OrderId).IsUnique();
        modelBuilder.Entity<OrderDetail>().HasIndex(x => new { x.OrderId, x.ProductId }).IsUnique();
        modelBuilder.Entity<ContainerTransaction>()
            .HasIndex(x => new { x.OrderId, x.ProductId }).IsUnique();
        modelBuilder.Entity<Payment>().HasIndex(x => new { x.OrderId, x.PaidAt });

        modelBuilder.Entity<Product>().ToTable("Products", table =>
        {
            table.HasCheckConstraint("CK_Products_Price_Positive", "[Price] > 0");
            table.HasCheckConstraint("CK_Products_Stock_NonNegative", "[StockQuantity] >= 0");
            table.HasCheckConstraint("CK_Products_MinimumStock_NonNegative", "[MinimumStock] >= 0");
            table.HasCheckConstraint("CK_Products_ContainerDeposit_NonNegative", "[ContainerDeposit] >= 0");
        });
        modelBuilder.Entity<Order>().ToTable("Orders", table =>
        {
            table.HasCheckConstraint("CK_Orders_TotalAmount_NonNegative", "[TotalAmount] >= 0");
            table.HasCheckConstraint("CK_Orders_ProductAmount_NonNegative", "[ProductAmount] >= 0");
            table.HasCheckConstraint("CK_Orders_DepositAmount_NonNegative", "[DepositAmount] >= 0");
            table.HasCheckConstraint("CK_Orders_PaidAmount_Valid", "[PaidAmount] >= 0 AND [PaidAmount] <= [TotalAmount]");
        });
        modelBuilder.Entity<OrderDetail>().ToTable("OrderDetails", table =>
        {
            table.HasCheckConstraint("CK_OrderDetails_Quantity_Positive", "[Quantity] > 0");
            table.HasCheckConstraint("CK_OrderDetails_UnitPrice_NonNegative", "[UnitPrice] >= 0");
            table.HasCheckConstraint("CK_OrderDetails_TotalPrice_NonNegative", "[TotalPrice] >= 0");
            table.HasCheckConstraint("CK_OrderDetails_ReturnedContainers_NonNegative", "[ReturnedContainerQuantity] >= 0");
            table.HasCheckConstraint("CK_OrderDetails_Deposit_NonNegative", "[ContainerDepositAmount] >= 0");
        });
        modelBuilder.Entity<Payment>().ToTable("Payments", table =>
            table.HasCheckConstraint("CK_Payments_Amount_Positive", "[Amount] > 0"));
        modelBuilder.Entity<ContainerTransaction>().ToTable("ContainerTransactions", table =>
        {
            table.HasCheckConstraint("CK_ContainerTransactions_Delivered_NonNegative", "[DeliveredQuantity] >= 0");
            table.HasCheckConstraint("CK_ContainerTransactions_Returned_NonNegative", "[ReturnedQuantity] >= 0");
            table.HasCheckConstraint("CK_ContainerTransactions_Balance_NonNegative", "[BalanceBefore] >= 0 AND [BalanceAfter] >= 0");
        });
        modelBuilder.Entity<InventoryTransaction>().ToTable("InventoryTransactions", table =>
        {
            table.HasCheckConstraint("CK_InventoryTransactions_Quantity_Positive", "[Quantity] > 0");
            table.HasCheckConstraint("CK_InventoryTransactions_Before_NonNegative", "[QuantityBefore] >= 0");
            table.HasCheckConstraint("CK_InventoryTransactions_After_NonNegative", "[QuantityAfter] >= 0");
        });

        modelBuilder.Entity<User>().Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Customer>().Property(x => x.CustomerType).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Order>().Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(30);
        modelBuilder.Entity<Order>().Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Order>().Property(x => x.OrderStatus).HasConversion<string>().HasMaxLength(30);
        modelBuilder.Entity<Delivery>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<InventoryTransaction>().Property(x => x.TransactionType).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Payment>().Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(30);

        modelBuilder.Entity<Order>()
            .HasOne(x => x.Customer)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Order>()
            .HasOne(x => x.CreatedByUser)
            .WithMany(x => x.CreatedOrders)
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Order>()
            .HasOne(x => x.DeliveryUser)
            .WithMany(x => x.AssignedOrders)
            .HasForeignKey(x => x.DeliveryUserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Order>()
            .HasOne(x => x.Delivery)
            .WithOne(x => x.Order)
            .HasForeignKey<Delivery>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Delivery>()
            .HasOne(x => x.DeliveryUser)
            .WithMany(x => x.Deliveries)
            .HasForeignKey(x => x.DeliveryUserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(x => x.Product)
            .WithMany(x => x.InventoryTransactions)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(x => x.Order)
            .WithMany(x => x.InventoryTransactions)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(x => x.CreatedByUser)
            .WithMany(x => x.InventoryTransactions)
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<OrderDetail>()
            .HasOne(x => x.Product)
            .WithMany(x => x.OrderDetails)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Payment>()
            .HasOne(x => x.Order).WithMany(x => x.Payments)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Payment>()
            .HasOne(x => x.RecordedByUser).WithMany(x => x.RecordedPayments)
            .HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ContainerTransaction>()
            .HasOne(x => x.Customer).WithMany(x => x.ContainerTransactions)
            .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ContainerTransaction>()
            .HasOne(x => x.Product).WithMany(x => x.ContainerTransactions)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ContainerTransaction>()
            .HasOne(x => x.Order).WithMany(x => x.ContainerTransactions)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ContainerTransaction>()
            .HasOne(x => x.CreatedByUser).WithMany(x => x.ContainerTransactions)
            .HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        foreach (var property in modelBuilder.Model.GetEntityTypes()
                     .SelectMany(entityType => entityType.GetProperties()))
        {
            if (property.ClrType == typeof(DateTime))
                property.SetValueConverter(new UtcDateTimeConverter());
            else if (property.ClrType == typeof(DateTime?))
                property.SetValueConverter(new NullableUtcDateTimeConverter());
        }
    }
}
