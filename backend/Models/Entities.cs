using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GiaPhuWaterDelivery.Api.Models;

public class User
{
    public int Id { get; set; }
    [MaxLength(150)] public string FullName { get; set; } = string.Empty;
    [MaxLength(50)] public string Username { get; set; } = string.Empty;
    [MaxLength(200)] public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Order> CreatedOrders { get; set; } = new List<Order>();
    public ICollection<Order> AssignedOrders { get; set; } = new List<Order>();
    public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
    public ICollection<Payment> RecordedPayments { get; set; } = new List<Payment>();
    public ICollection<ContainerTransaction> ContainerTransactions { get; set; } = new List<ContainerTransaction>();
}

public class ProductCategory
{
    public int Id { get; set; }
    [MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product
{
    public int Id { get; set; }
    [MaxLength(30)] public string ProductCode { get; set; } = string.Empty;
    [MaxLength(150)] public string ProductName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public ProductCategory Category { get; set; } = null!;
    [MaxLength(50)] public string Capacity { get; set; } = string.Empty;
    [MaxLength(50)] public string Unit { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")] public decimal Price { get; set; }
    public bool RequiresContainer { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal ContainerDeposit { get; set; }
    public int StockQuantity { get; set; }
    public int MinimumStock { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
    public ICollection<ContainerTransaction> ContainerTransactions { get; set; } = new List<ContainerTransaction>();
}

public class Customer
{
    public int Id { get; set; }
    [MaxLength(30)] public string CustomerCode { get; set; } = string.Empty;
    [MaxLength(200)] public string CustomerName { get; set; } = string.Empty;
    [MaxLength(20)] public string Phone { get; set; } = string.Empty;
    [MaxLength(150)] public string? Email { get; set; }
    [MaxLength(500)] public string Address { get; set; } = string.Empty;
    public CustomerType CustomerType { get; set; }
    public int ContainerBalance { get; set; }
    [MaxLength(1000)] public string? Note { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<ContainerTransaction> ContainerTransactions { get; set; } = new List<ContainerTransaction>();
}

public class Order
{
    public int Id { get; set; }
    [MaxLength(30)] public string OrderCode { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public int? DeliveryUserId { get; set; }
    public User? DeliveryUser { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDeliveryDate { get; set; }
    [MaxLength(500)] public string DeliveryAddress { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")] public decimal TotalAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal ProductAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal DepositAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal PaidAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public OrderStatus OrderStatus { get; set; } = OrderStatus.New;
    [MaxLength(1000)] public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<OrderDetail> Details { get; set; } = new List<OrderDetail>();
    public Delivery? Delivery { get; set; }
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<ContainerTransaction> ContainerTransactions { get; set; } = new List<ContainerTransaction>();
}

public class OrderDetail
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TotalPrice { get; set; }
    public int ReturnedContainerQuantity { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal ContainerDepositAmount { get; set; }
}

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public int RecordedByUserId { get; set; }
    public User RecordedByUser { get; set; } = null!;
    [MaxLength(1000)] public string? Note { get; set; }
}

public class ContainerTransaction
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int DeliveredQuantity { get; set; }
    public int ReturnedQuantity { get; set; }
    public int BalanceBefore { get; set; }
    public int BalanceAfter { get; set; }
    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(1000)] public string? Note { get; set; }
}

public class Delivery
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int DeliveryUserId { get; set; }
    public User DeliveryUser { get; set; } = null!;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Assigned;
    [MaxLength(1000)] public string? FailureReason { get; set; }
    [MaxLength(1000)] public string? Note { get; set; }
}

public class InventoryTransaction
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    public InventoryTransactionType TransactionType { get; set; }
    public int Quantity { get; set; }
    public int QuantityBefore { get; set; }
    public int QuantityAfter { get; set; }
    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(1000)] public string? Note { get; set; }
}
