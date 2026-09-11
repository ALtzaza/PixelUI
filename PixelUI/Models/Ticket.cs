using System;
using System.Collections.Generic;

namespace PixelUI.Models.Db;

public partial class Ticket
{
    public int TicketId { get; set; }

    public int? UserId { get; set; }

    public string? Subject { get; set; }

    public string? Message { get; set; }

    public string? Category { get; set; }   // NEW

    public string? Priority { get; set; }   // NEW


    public string Status { get; set; } = "Open"; 

     public int? AssignedToUserId { get; set; } 

    public DateTime CreatedAt { get; set; } = DateTime.Now; // UPDATED

    public DateTime? UpdatedAt { get; set; } // NEW

    public DateTime? ClosedAt { get; set; }  // NEW

    public bool IsHidden { get; set; } = false;

    public int? ProductId { get; set; }

// Navigation
    public virtual User? User { get; set; }

    public Product? Product { get; set; }

    public User? AssignedToUser { get; set; } // NEW

    public ICollection<TicketMessage> Messages { get; set; }
    public Ticket()
    {
        Messages = new List<TicketMessage>();
    }
    
}
