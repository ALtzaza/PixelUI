using PixelUI.Models.Db; // ตรวจสอบให้แน่ใจว่าเรียกใช้ Namespace ของ Model Product

namespace PixelUI.ViewModels
{
    public class ProductDetailViewModel
    {
        // ต้องมี Property นี้เพื่อให้ View เรียก @Model.Product ได้
        public Product Product { get; set; }

        // ต้องมี Property นี้เพื่อให้ View เรียก @Model.ScopedCssCode ได้
        public string ScopedCssCode { get; set; }

        // User information and role
        public User CurrentUser { get; set; }
        public string UserRole { get; set; } // "Admin", "Member", "Guest"
        
        // Purchase status
        public bool IsPurchased { get; set; }
        public bool IsLoggedIn { get; set; }
        
        // Browser compatibility info
        public List<BrowserCompatibility> BrowserCompatibility { get; set; } = new List<BrowserCompatibility>();
    }

    public class BrowserCompatibility
    {
        public string BrowserName { get; set; }
        public string MinVersion { get; set; }
        public bool IsSupported { get; set; }
    }
}