using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payroll.Data;
using Payroll.Filters;
using Payroll.Models;

namespace Payroll.Controllers;

[Authorize]
[RequireModuleAccess(Modules.PaymentSchemes)]
public class PaymentSchemeController(AppDbContext db) : Controller
{
    private const int PageSize = 10;

    public async Task<IActionResult> Index(int page = 1, string? search = null, string? status = null)
    {
        ViewData["PageTitle"] = "Payment Schemes";
        ViewData["PageSubtitle"] = "Manage salary structures and payment rules";

        var query = db.PaymentSchemes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => s.Name.Contains(search));

        if (status == "active")   query = query.Where(s => s.IsActive);
        if (status == "inactive") query = query.Where(s => !s.IsActive);

        query = query.OrderBy(s => s.Name);

        var total = await query.CountAsync();
        var schemes = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Page       = page;
        ViewBag.PageSize   = PageSize;
        ViewBag.TotalCount = total;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        ViewBag.Search     = search;
        ViewBag.Status     = status;

        return View(schemes);
    }

    public IActionResult Create()
    {
        ViewData["PageTitle"] = "Add Payment Scheme";
        ViewData["PageSubtitle"] = "Create a new salary structure";
        return View(new PaymentScheme { IsActive = true });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PaymentScheme model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["PageTitle"] = "Add Payment Scheme";
            ViewData["PageSubtitle"] = "Create a new salary structure";
            return View(model);
        }

        db.PaymentSchemes.Add(model);
        await db.SaveChangesAsync();
        TempData["Success"] = $"Payment scheme \"{model.Name}\" created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var scheme = await db.PaymentSchemes.FindAsync(id);
        if (scheme == null) return NotFound();

        ViewData["PageTitle"] = "Edit Payment Scheme";
        ViewData["PageSubtitle"] = "Update salary structure details";
        return View(scheme);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PaymentScheme model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            ViewData["PageTitle"] = "Edit Payment Scheme";
            ViewData["PageSubtitle"] = "Update salary structure details";
            return View(model);
        }

        var scheme = await db.PaymentSchemes.FindAsync(id);
        if (scheme == null) return NotFound();

        scheme.Name = model.Name;
        scheme.BasicSalary = model.BasicSalary;
        scheme.OvertimeRatePerHour = model.OvertimeRatePerHour;
        scheme.AllowanceAmount = model.AllowanceAmount;
        scheme.IsActive = model.IsActive;

        await db.SaveChangesAsync();
        TempData["Success"] = $"Payment scheme \"{scheme.Name}\" updated successfully.";
        return RedirectToAction(nameof(Index));
    }
}
