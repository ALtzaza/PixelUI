using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PixelUI.Models.Db;

public partial class Promotion
{
    public int PromotionId { get; set; }

    public string? PromoCode { get; set; }

    public int? DiscountPct { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public string? PromotionType { get; set; } // StarterPack, BundleDeal, Subscription, StudentLicense, UpdateGuarantee

    public string? Description { get; set; }

    public string? Conditions { get; set; } // เช่น new customers ONLY, .edu/.ac.th email only

    [Column("BundleProductIds")]
    public string? BundleProductIds { get; set; } // เก็บ ProductIds ที่เป็น Bundle คั่นด้วย comma

    public string? Status { get; set; } // Active, Inactive, Expired

    public DateTime? CreatedDate { get; set; }

    public DateTime? StartDate { get; set; }

    public int? MaxUsageCount { get; set; } // จำนวนครั้งที่ใช้ได้สูงสุด

    public int? CurrentUsageCount { get; set; } // นับจำนวนที่ใช้ไปแล้ว
}
