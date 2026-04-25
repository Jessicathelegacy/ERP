using System.ComponentModel.DataAnnotations;
using Payroll.Models;

namespace Payroll.ViewModels;

public class PayrollListViewModel
{
    public int Id { get; set; }
    public string EmployeeName { get; set; } = "";
    public DateTime PayPeriodStart { get; set; }
    public DateTime PayPeriodEnd { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal Allowances { get; set; }
    public decimal Deductions { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal NetSalary { get; set; }
    public PayrollStatus Status { get; set; }
}

public class PayrollFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please select an employee.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select an employee.")]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime PayPeriodStart { get; set; }

    [Required]
    public DateTime PayPeriodEnd { get; set; }

    public decimal BasicSalary { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal Allowances { get; set; }
    public decimal Deductions { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal NetSalary { get; set; }

    public PayrollStatus Status { get; set; } = PayrollStatus.Draft;

    [MaxLength(500)]
    public string? Remarks { get; set; }
}
