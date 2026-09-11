using System;
using System.Collections.Generic;

namespace PixelUI.Models.Db;

public partial class Userlicense
{
    public int LicenseId { get; set; }

    public int? UserId { get; set; }

    public int? ProductId { get; set; }

    public bool? IsLifetime { get; set; }

    public DateTime? GrantedDate { get; set; }
}
