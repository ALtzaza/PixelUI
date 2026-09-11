using System;
using System.Collections.Generic;

namespace PixelUI.Models.Db;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public decimal? BasePrice { get; set; }

    public int TierLevel { get; set; } = 2; 

    public int TemplateType { get; set; }

    public string? SectionType { get; set; } 

    public string? PreviewImage { get; set; }

    public int? ComplexityTier { get; set; }

    public string? TechStack { get; set; }

    public string? HtmlCode { get; set; }

    public string? CssCode { get; set; }

    public string? Author { get; set; }

    public string? Category { get; set; }

    public int ViewCount { get; set; } = 0;

    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

    public string? Description { get; set; }

    public string? IncludedItems { get; set; }

    public string? Highlights { get; set; }

    public string? Format { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
