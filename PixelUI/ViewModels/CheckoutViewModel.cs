namespace PixelUI.ViewModels
{
    public class CheckoutViewModel
    {
        public int ProductId { get; set; }
        public int UserId { get; set; }
        public string ProductName { get; set; }
        public decimal ProductPrice { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public string LicenseType { get; set; } = "Personal";
        // Bundle support
        public string? BundleProductIds { get; set; }
        public bool IsBundle { get; set; }
        
        // Payment Info
        public string CardNumber { get; set; }
        public string CardName { get; set; }
        public string ExpiryMonth { get; set; }
        public string ExpiryYear { get; set; }
        public string CVV { get; set; }
        
        // Promo Code
        public string AppliedPromoCode { get; set; }
        public int AppliedDiscount { get; set; }
    }
}
