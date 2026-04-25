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
[RequireModuleAccess(Modules.Employees)]
public class EmployeeController(AppDbContext db) : Controller
{
    private const int PageSize = 10;

    public async Task<IActionResult> Index(int page = 1, string? search = null, int? schemeId = null, string? status = null)
    {
        ViewData["PageTitle"] = "Employees";
        ViewData["PageSubtitle"] = "Manage employee records";

        var query = db.Employees.Include(e => e.PaymentScheme).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => e.Name.Contains(search) || e.Email.Contains(search));

        if (schemeId.HasValue)
            query = query.Where(e => e.PaymentSchemeId == schemeId.Value);

        if (status == "active")   query = query.Where(e => e.IsActive);
        if (status == "inactive") query = query.Where(e => !e.IsActive);

        query = query.OrderBy(e => e.Name);

        var total = await query.CountAsync();
        var employees = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Page       = page;
        ViewBag.PageSize   = PageSize;
        ViewBag.TotalCount = total;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        ViewBag.Search     = search;
        ViewBag.SchemeId   = schemeId;
        ViewBag.Status     = status;
        ViewBag.Schemes    = new SelectList(
            await db.PaymentSchemes.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(),
            "Id", "Name", schemeId);

        return View(employees);
    }

    public async Task<IActionResult> Create()
    {
        ViewData["PageTitle"] = "Add Employee";
        ViewData["PageSubtitle"] = "Register a new employee";
        await PopulateSchemes();
        return View(new Employee { JoinDate = DateTime.Today, IsActive = true });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Employee model)
    {
        if (await db.Employees.AnyAsync(e => e.Email == model.Email))
            ModelState.AddModelError(nameof(model.Email), "This email is already registered.");

        if (!ModelState.IsValid)
        {
            ViewData["PageTitle"] = "Add Employee";
            ViewData["PageSubtitle"] = "Register a new employee";
            await PopulateSchemes(model.PaymentSchemeId);
            return View(model);
        }

        db.Employees.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Employee \"{model.Name}\" created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var employee = await db.Employees.FindAsync(id);
        if (employee == null) return NotFound();

        ViewData["PageTitle"] = "Edit Employee";
        ViewData["PageSubtitle"] = "Update employee details";
        await PopulateSchemes(employee.PaymentSchemeId);

        var vm = new EmployeeEditViewModel
        {
            Employee = employee,
            PayrollHistory = await db.Payrolls
                .Where(p => p.EmployeeId == id)
                .OrderByDescending(p => p.PayPeriodStart)
                .ToListAsync()
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmployeeEditViewModel vm)
    {
        var model = vm.Employee;
        if (id != model.Id) return BadRequest();

        if (await db.Employees.AnyAsync(e => e.Email == model.Email && e.Id != id))
            ModelState.AddModelError("Employee.Email", "This email is already registered.");

        if (!ModelState.IsValid)
        {
            ViewData["PageTitle"] = "Edit Employee";
            ViewData["PageSubtitle"] = "Update employee details";
            await PopulateSchemes(model.PaymentSchemeId);

            vm.PayrollHistory = await db.Payrolls
                .Where(p => p.EmployeeId == id)
                .OrderByDescending(p => p.PayPeriodStart)
                .ToListAsync();

            return View(vm);
        }

        var employee = await db.Employees.FindAsync(id);
        if (employee == null) return NotFound();

        employee.Name = model.Name;
        employee.Email = model.Email;
        employee.Phone = model.Phone;
        employee.PaymentSchemeId = model.PaymentSchemeId;
        employee.JoinDate = model.JoinDate;
        employee.IsActive = model.IsActive;

        await db.SaveChangesAsync();
        TempData["Success"] = $"Employee \"{employee.Name}\" updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSchemes(int? selectedId = null)
    {
        var schemes = await db.PaymentSchemes
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync();

        ViewBag.PaymentSchemes = new SelectList(schemes, "Id", "Name", selectedId);
    }
}
