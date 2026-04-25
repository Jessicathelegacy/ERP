using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Payroll.Models;

public enum PayrollStatus
{
    Draft,
    Approved,
    Paid
}

public class Payroll
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int? PayrollBatchId { get; set; }
    public PayrollBatch? PayrollBatch { get; set; }

    public DateTime PayPeriodStart { get; set; }
    public DateTime PayPeriodEnd { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BasicSalary { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OvertimeHours { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OvertimePay { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Allowances { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Deductions { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal GrossSalary { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NetSalary { get; set; }

    public PayrollStatus Status { get; set; } = PayrollStatus.Draft;

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
