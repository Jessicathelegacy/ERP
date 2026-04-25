using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Payroll.Data;
using Payroll.Filters;
using Payroll.Models;
using Payroll.ViewModels;

namespace Payroll.Controllers;

[Authorize]
[RequireModuleAccess(Modules.Payroll)]
public class PayrollController(AppDbContext db) : Controller
{
    private const int PageSize = 10;

    public async Task<IActionResult> Index(int page = 1, string? search = null, int? year = null, int? month = null, string? status = null)
    {
        ViewData["PageTitle"] = "Payroll";
        ViewData["PageSubtitle"] = "Manage individual payroll records";

        var query = db.Payrolls.Include(p => p.Employee).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Employee.Name.Contains(search));

        if (year.HasValue)
            query = query.Where(p => p.PayPeriodStart.Year == year.Value);

        if (month.HasValue)
            query = query.Where(p => p.PayPeriodStart.Month == month.Value);

        if (status == "draft")    query = query.Where(p => p.Status == PayrollStatus.Draft);
        if (status == "approved") query = query.Where(p => p.Status == PayrollStatus.Approved);
        if (status == "paid")     query = query.Where(p => p.Status == PayrollStatus.Paid);

        query = query.OrderByDescending(p => p.PayPeriodStart).ThenBy(p => p.Employee.Name);

        var total = await query.CountAsync();
        var payrolls = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(p => new PayrollListViewModel
            {
                Id             = p.Id,
                EmployeeName   = p.Employee.Name,
                PayPeriodStart = p.PayPeriodStart,
                PayPeriodEnd   = p.PayPeriodEnd,
                BasicSalary    = p.BasicSalary,
                OvertimePay    = p.OvertimePay,
                Allowances     = p.Allowances,
                Deductions     = p.Deductions,
                GrossSalary    = p.GrossSalary,
                NetSalary      = p.NetSalary,
                Status         = p.Status
            })
            .ToListAsync();

        var years = await db.Payrolls
            .Select(p => p.PayPeriodStart.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();

        ViewBag.Page       = page;
        ViewBag.PageSize   = PageSize;
        ViewBag.TotalCount = total;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        ViewBag.Search     = search;
        ViewBag.Year       = year;
        ViewBag.Month      = month;
        ViewBag.Status     = status;
        ViewBag.Years      = years;

        return View(payrolls);
    }

    public async Task<IActionResult> Create()
    {
        ViewData["PageTitle"] = "Add Payroll";
        ViewData["PageSubtitle"] = "Create a new payroll record";
        await PopulateDropdowns();

        var today = DateTime.Today;
        return View(new PayrollFormViewModel
        {
            PayPeriodStart = new DateTime(today.Year, today.Month, 1),
            PayPeriodEnd = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month))
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PayrollFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewData["PageTitle"] = "Add Payroll";
            ViewData["PageSubtitle"] = "Create a new payroll record";
            await PopulateDropdowns(vm.EmployeeId);
            return View(vm);
        }

        var record = MapToEntity(vm);
        db.Payrolls.Add(record);
        await db.SaveChangesAsync();
        TempData["Success"] = "Payroll record created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var record = await db.Payrolls.FindAsync(id);
        if (record == null) return NotFound();

        ViewData["PageTitle"] = "Edit Payroll";
        ViewData["PageSubtitle"] = "Update payroll record details";
        await PopulateDropdowns(record.EmployeeId);

        return View(new PayrollFormViewModel
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            PayPeriodStart = record.PayPeriodStart,
            PayPeriodEnd = record.PayPeriodEnd,
            BasicSalary = record.BasicSalary,
            OvertimeHours = record.OvertimeHours,
            OvertimePay = record.OvertimePay,
            Allowances = record.Allowances,
            Deductions = record.Deductions,
            GrossSalary = record.GrossSalary,
            NetSalary = record.NetSalary,
            Status = record.Status,
            Remarks = record.Remarks
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PayrollFormViewModel vm)
    {
        if (id != vm.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            ViewData["PageTitle"] = "Edit Payroll";
            ViewData["PageSubtitle"] = "Update payroll record details";
            await PopulateDropdowns(vm.EmployeeId);
            return View(vm);
        }

        var record = await db.Payrolls.FindAsync(id);
        if (record == null) return NotFound();

        record.EmployeeId = vm.EmployeeId;
        record.PayPeriodStart = vm.PayPeriodStart;
        record.PayPeriodEnd = vm.PayPeriodEnd;
        record.BasicSalary = vm.BasicSalary;
        record.OvertimeHours = vm.OvertimeHours;
        record.OvertimePay = vm.OvertimePay;
        record.Allowances = vm.Allowances;
        record.Deductions = vm.Deductions;
        record.GrossSalary = vm.GrossSalary;
        record.NetSalary = vm.NetSalary;
        record.Status = vm.Status;
        record.Remarks = vm.Remarks;

        await db.SaveChangesAsync();
        TempData["Success"] = "Payroll record updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> GetEmployeeDefaults(int employeeId)
    {
        var employee = await db.Employees
            .Include(e => e.PaymentScheme)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null) return NotFound();

        return Json(new
        {
            basicSalary = employee.PaymentScheme.BasicSalary,
            overtimeRatePerHour = employee.PaymentScheme.OvertimeRatePerHour,
            allowances = employee.PaymentScheme.AllowanceAmount
        });
    }

    private async Task PopulateDropdowns(int? selectedEmployeeId = null)
    {
        var employees = await db.Employees
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name)
            .ToListAsync();

        ViewBag.Employees = new SelectList(employees, "Id", "Name", selectedEmployeeId);
    }

    private static Models.Payroll MapToEntity(PayrollFormViewModel vm) => new()
    {
        EmployeeId = vm.EmployeeId,
        PayPeriodStart = vm.PayPeriodStart,
        PayPeriodEnd = vm.PayPeriodEnd,
        BasicSalary = vm.BasicSalary,
        OvertimeHours = vm.OvertimeHours,
        OvertimePay = vm.OvertimePay,
        Allowances = vm.Allowances,
        Deductions = vm.Deductions,
        GrossSalary = vm.GrossSalary,
        NetSalary = vm.NetSalary,
        Status = vm.Status,
        Remarks = vm.Remarks,
        CreatedAt = DateTime.UtcNow
    };
}
