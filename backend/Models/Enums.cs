namespace GiaPhuWaterDelivery.Api.Models;

public enum UserRole
{
    Admin,
    Sales,
    Warehouse,
    Delivery
}

public enum OrderStatus
{
    New,
    Confirmed,
    Preparing,
    ReadyForDelivery,
    Delivering,
    Delivered,
    DeliveryFailed,
    Cancelled
}

public enum DeliveryStatus
{
    Pending,
    Assigned,
    Delivering,
    Completed,
    Failed
}

public enum PaymentMethod
{
    Cash,
    BankTransfer,
    CashOnDelivery
}

public enum PaymentStatus
{
    Unpaid,
    Partial,
    Paid
}

public enum CustomerType
{
    Individual,
    Dealer,
    Company,
    School,
    Restaurant
}

public enum InventoryTransactionType
{
    StockIn,
    StockOut
}
