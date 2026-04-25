using Payroll.Models;

namespace Payroll.ViewModels;

public class PayslipViewModel
{
    public int PayrollId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string EmployeeEmail { get; set; } = "";
    public string? EmployeePhone { get; set; }
    public DateTime JoinDate { get; set; }
    public string PaymentSchemeName { get; set; } = "";
    public DateTime PayPeriodStart { get; set; }
    public DateTime PayPeriodEnd { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal OvertimeRatePerHour { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal Allowances { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal Deductions { get; set; }
    public decimal NetSalary { get; set; }
    public PayrollStatus Status { get; set; }
    public string? Remarks { get; set; }
    public string? BatchDescription { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}
