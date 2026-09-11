using System;
using System.Collections.Generic;

namespace PixelUI.Models.Db;

public partial class Orderitem
{
    public int OrderItemId { get; set; }

    public int? OrderId { get; set; }

    public int? ProductId { get; set; }

    public decimal? PriceAtBuy { get; set; }
}
