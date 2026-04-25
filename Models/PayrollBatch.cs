using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Payroll.Models;

public enum PayrollBatchStatus
{
    Draft,
    Processing,
    Completed,
    Cancelled,
    Submitted,  // 4 — submitted for approval
    Rejected    // 5 — rejected by approver, returned to submitter
}

public class PayrollBatch
{
    public int Id { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    [MaxLength(300)]
    public string? Description { get; set; }

    public PayrollBatchStatus Status { get; set; } = PayrollBatchStatus.Draft;

    public int TotalEmployees { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalGrossSalary { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalDeductions { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalNetSalary { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    public int? SubmittedByAdminUserId { get; set; }
    public AdminUser? SubmittedByAdminUser { get; set; }

    public DateTime? SubmittedAt { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    public ICollection<Payroll> Payrolls { get; set; } = [];
    public ICollection<PayrollBatchEvent> Events { get; set; } = [];
}
