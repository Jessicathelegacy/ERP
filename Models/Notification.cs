using System.ComponentModel.DataAnnotations;

namespace Payroll.Models;

public class Notification
{
    public int Id { get; set; }

    public int RecipientAdminUserId { get; set; }
    public AdminUser RecipientAdminUser { get; set; } = null!;

    [MaxLength(200)]
    public string Title { get; set; } = "";

    [MaxLength(500)]
    public string Message { get; set; } = "";

    [MaxLength(300)]
    public string? LinkUrl { get; set; }

    /// <summary>info | success | warning | danger</summary>
    [MaxLength(20)]
    public string Type { get; set; } = "info";

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
