namespace PixelUI.ViewModels
{
    public class PromotionViewModel
    {
        public int PromotionId { get; set; }

        public string? PromoCode { get; set; }

        public int? DiscountPct { get; set; }

        public string? PromotionType { get; set; }
        // Options: StarterPack, BundleDeal, Subscription, StudentLicense, UpdateGuarantee

        public string? Description { get; set; }

        public string? Conditions { get; set; }
        // Examples: "New Customers Only", "Student Email Only (.edu/.ac.th)", "All Customers"

        public string? BundleProductIds { get; set; }
        // Comma-separated product IDs for bundle deals

        public string? Status { get; set; }
        // Options: Active, Inactive

        public DateTime? StartDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public int? MaxUsageCount { get; set; }

        // Products list for selecting bundle items
        public List<ProductDropdownItem>? AvailableProducts { get; set; }

        public List<int>? SelectedProductIds { get; set; }
    }

    public class ProductDropdownItem
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal? BasePrice { get; set; }
        public string? Category { get; set; }
    }
}
