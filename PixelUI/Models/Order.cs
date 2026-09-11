using System;
using System.Collections.Generic;

namespace PixelUI.Models.Db;

public partial class Order
{
    public int OrderId { get; set; }

    public int? UserId { get; set; }

    public int? ProductId { get; set; }

    public string? LicenseType { get; set; }

    public decimal? FinalPrice { get; set; }

    public DateTime? PurchaseDate { get; set; }

    public virtual Product? Product { get; set; }

    public virtual User? User { get; set; }
}
