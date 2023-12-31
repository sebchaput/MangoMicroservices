namespace Mango.Web.Utility
{

    public class SD     // Static Details
    {
        public static string CouponAPIBase { get; set; }
        public static string AuthAPIBase { get; set; }
        public static string ProductAPIBase { get; set; }
        public static string ShoppingCartAPIBase { get; set; }
        public static string OrderAPIBase { get; set; }

        public const string RoleAdmin = "ADMIN";
        public const string RoleCustomer = "CUSTOMER";

        public const string TokenCookie = "JWTToken";

        public enum ApiType
        {
            GET, 
            POST, 
            PUT, 
            DELETE
        }

        public const string Status_Pending = "Pending";                 // Initially order is Pending
        public const string Status_Approved = "Approved";               // When payment is confirmed
        public const string Status_ReadyForPickup = "ReadyForPickup";   // When it's cooked
        public const string Status_Completed = "Completed";             // When customer picked up order
        public const string Status_Refunded = "Refunded";               // If order is cancelled after payment
        public const string Status_Cancelled = "Cancelled";             // If order is cancelled before payment

        public enum ContentType
        {
            Json,
            MultipartFormData
        }
    }
}
