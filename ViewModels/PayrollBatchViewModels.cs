using System.ComponentModel.DataAnnotations;
using Payroll.Models;

namespace Payroll.ViewModels;

public class PayrollBatchEditViewModel
{
    public PayrollBatch Batch { get; set; } = null!;
    public List<BatchPayrollItem> PayrollItems { get; set; } = [];
    public List<PayrollBatchEvent> Events { get; set; } = [];
}

public class BatchApprovalReviewViewModel
{
    public PayrollBatch Batch { get; set; } = null!;
    public List<BatchPayrollItem> PayrollItems { get; set; } = [];
    public string? SubmittedByUsername { get; set; }
    public bool IsOwnSubmission { get; set; }
}

public class RejectBatchViewModel
{
    public int BatchId { get; set; }

    [Required(ErrorMessage = "Please provide a rejection reason.")]
    [MaxLength(500)]
    public string Reason { get; set; } = "";
}

public class BatchPayrollItem
{
    public int Id { get; set; }
    public string EmployeeName { get; set; } = "";
    public decimal BasicSalary { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal OvertimeRatePerHour { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal Allowances { get; set; }
    public decimal Deductions { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal NetSalary { get; set; }
    public PayrollStatus Status { get; set; }
}

public class UpdateDeductionsViewModel
{
    public int BatchId { get; set; }
    public List<DeductionItem> Items { get; set; } = [];
}

public class DeductionItem
{
    public int PayrollId { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal OvertimeRatePerHour { get; set; }
    public decimal Deductions { get; set; }
}
