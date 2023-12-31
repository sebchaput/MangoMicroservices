namespace Mango.Services.OrderAPI.Utility
{
    public class SD
    {
        public const string Status_Pending = "Pending";                 // Initially order is Pending
        public const string Status_Approved = "Approved";               // When payment is confirmed
        public const string Status_ReadyForPickup = "ReadyForPickup";   // When it's cooked
        public const string Status_Completed = "Completed";             // When customer picked up order
        public const string Status_Refunded = "Refunded";               // If order is cancelled after payment
        public const string Status_Cancelled = "Cancelled";             // If order is cancelled before payment

        public const string RoleAdmin = "ADMIN";
        public const string RoleCustomer = "CUSTOMER";
    }
}
