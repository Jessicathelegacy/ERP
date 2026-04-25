using System.ComponentModel.DataAnnotations;

namespace Payroll.Models;

public class PayrollBatchEvent
{
    public int Id { get; set; }

    public int PayrollBatchId { get; set; }
    public PayrollBatch PayrollBatch { get; set; } = null!;

    // Drafted | Submitted | Recalled | Approved | Rejected
    [MaxLength(50)]
    public string EventType { get; set; } = "";

    public int? AdminUserId { get; set; }
    public AdminUser? AdminUser { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
