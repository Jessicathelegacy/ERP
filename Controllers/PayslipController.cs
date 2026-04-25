using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payroll.Data;
using Payroll.Filters;
using Payroll.Models;
using Payroll.ViewModels;

namespace Payroll.Controllers;

[Authorize]
[RequireModuleAccess(Modules.Payslip)]
public class PayslipController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Print(int id)
    {
        var p = await db.Payrolls
            .Include(x => x.Employee).ThenInclude(e => e.PaymentScheme)
            .Include(x => x.PayrollBatch)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (p == null) return NotFound();

        var vm = new PayslipViewModel
        {
            PayrollId           = p.Id,
            EmployeeName        = p.Employee.Name,
            EmployeeEmail       = p.Employee.Email,
            EmployeePhone       = p.Employee.Phone,
            JoinDate            = p.Employee.JoinDate,
            PaymentSchemeName   = p.Employee.PaymentScheme.Name,
            PayPeriodStart      = p.PayPeriodStart,
            PayPeriodEnd        = p.PayPeriodEnd,
            BasicSalary         = p.BasicSalary,
            OvertimeHours       = p.OvertimeHours,
            OvertimeRatePerHour = p.Employee.PaymentScheme.OvertimeRatePerHour,
            OvertimePay         = p.OvertimePay,
            Allowances          = p.Allowances,
            GrossSalary         = p.GrossSalary,
            Deductions          = p.Deductions,
            NetSalary           = p.NetSalary,
            Status              = p.Status,
            Remarks             = p.Remarks,
            BatchDescription    = p.PayrollBatch?.Description,
            GeneratedAt         = DateTime.Now
        };

        return View(vm);
    }
}
