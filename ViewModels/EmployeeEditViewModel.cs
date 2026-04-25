using Payroll.Models;
using PayrollRecord = Payroll.Models.Payroll;

namespace Payroll.ViewModels;

public class EmployeeEditViewModel
{
    public Employee Employee { get; set; } = null!;
    public List<PayrollRecord> PayrollHistory { get; set; } = [];
}
