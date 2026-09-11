using System;
using System.Collections.Generic;

namespace PixelUI.Models.Db;

public class TicketMessage
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public int? UserId { get; set; } // คนส่ง

    public string? Message { get; set; }

    public bool IsAdmin { get; set; } // true = admin

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Optional attachment path (stored under wwwroot/uploads/tickets/...)
    public string? AttachmentPath { get; set; }

    // Navigation
    public Ticket Ticket { get; set; }
    public User? User { get; set; }
}