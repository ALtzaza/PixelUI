using System;
using System.Collections.Generic;

namespace PixelUI.Models.Db;

public class StudentVerification
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string StudentId { get; set; } = null!;

    public string University { get; set; } = null!;

    public string ProofImage { get; set; } = null!; // รูปบัตร

    public string Status { get; set; } = "Pending"; 
    // Pending / Approved / Rejected

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual User User { get; set; }
}