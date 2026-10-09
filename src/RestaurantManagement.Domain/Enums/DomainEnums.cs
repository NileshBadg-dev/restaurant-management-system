namespace RestaurantManagement.Domain.Enums;

public enum UserRole
{
    Admin = 1,
    Manager = 2,
    Cashier = 3,
    Waiter = 4,
    KitchenStaff = 5,
    InventoryStaff = 6
}

public enum TableStatus
{
    Available = 1,
    Occupied = 2,
    Reserved = 3,
    Billing = 4,
    Cleaning = 5,
    OutOfService = 6
}

public enum OrderType
{
    DineIn = 1,
    Takeaway = 2,
    Delivery = 3
}

public enum OrderStatus
{
    Pending = 1,
    Confirmed = 2,
    Preparing = 3,
    Ready = 4,
    Completed = 5,
    Cancelled = 6
}

public enum KOTStatus
{
    New = 1,
    Accepted = 2,
    Preparing = 3,
    Ready = 4,
    Served = 5
}

public enum PaymentMethod
{
    Cash = 1,
    UPI = 2,
    CreditCard = 3,
    DebitCard = 4,
    Wallet = 5,
    Split = 6
}

public enum PaymentStatus
{
    Pending = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Refunded = 4,
    Failed = 5
}

public enum ReservationStatus
{
    Pending = 1,
    Confirmed = 2,
    Seated = 3,
    Completed = 4,
    Cancelled = 5,
    NoShow = 6
}

public enum LoyaltyTier
{
    Silver = 1,
    Gold = 2,
    Platinum = 3
}
