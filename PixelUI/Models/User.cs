using System;
using System.Collections.Generic;

namespace PixelUI.Models.Db;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int? RoleId { get; set; }

    public string? Password { get; set; }

    public string? ResetToken { get; set; }

    public bool IsStudentVerified { get; set; } = false;
    
    public DateTime? ResetTokenExpiry { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual Role? Role { get; set; }
}
