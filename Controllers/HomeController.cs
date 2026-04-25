using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Payroll.Data;
using Payroll.Filters;
using Payroll.Models;
using Payroll.ViewModels;

namespace Payroll.Controllers;

[Authorize]
public class HomeController(AppDbContext db, IMemoryCache cache) : Controller
{
    [RequireModuleAccess(Modules.Dashboard)]
    public async Task<IActionResult> Index()
    {
        ViewData["PageTitle"] = "Dashboard";
        ViewData["PageSubtitle"] = "Welcome to Payroll System";

        var granted = await GetGrantedModulesAsync();

        var today      = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd   = monthStart.AddMonths(1).AddDays(-1);

        var vm = new DashboardViewModel
        {
            GrantedModules   = granted,
            CurrentMonthName = today.ToString("MMMM yyyy")
        };

        if (granted.Contains(Modules.Employees))
        {
            vm.ActiveEmployees     = await db.Employees.CountAsync(e => e.IsActive);
            vm.TotalPaymentSchemes = await db.PaymentSchemes.CountAsync(s => s.IsActive);
        }

        if (granted.Contains(Modules.Payroll))
        {
            vm.CurrentMonthNetPayout = await db.Payrolls
                .Where(p => p.PayPeriodStart >= monthStart && p.PayPeriodEnd <= monthEnd)
                .SumAsync(p => (decimal?)p.NetSalary) ?? 0;
            vm.DraftPayrolls = await db.Payrolls.CountAsync(p => p.Status == PayrollStatus.Draft);
        }

        if (granted.Contains(Modules.BatchPayroll))
        {
            vm.PendingBatches = await db.PayrollBatches.CountAsync(b =>
                b.Status == PayrollBatchStatus.Draft || b.Status == PayrollBatchStatus.Processing);

            var latest = await db.PayrollBatches
                .OrderByDescending(b => b.Year).ThenByDescending(b => b.Month)
                .FirstOrDefaultAsync();

            if (latest != null)
            {
                vm.LatestBatchPeriod = new DateTime(latest.Year, latest.Month, 1).ToString("MMMM yyyy");
                vm.LatestBatchStatus = latest.Status;
            }
        }

        if (granted.Contains(Modules.BatchPayrollApproval))
        {
            vm.SubmittedBatches = await db.PayrollBatches.CountAsync(b =>
                b.Status == PayrollBatchStatus.Submitted);
        }

        return View(vm);
    }

    private async Task<HashSet<string>> GetGrantedModulesAsync()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
            return [];

        var version  = cache.Get<long>("perm_version");
        var cacheKey = $"nav_modules:{userId}:{version}";

        if (!cache.TryGetValue(cacheKey, out HashSet<string>? modules))
        {
            var perms = await db.AdminUserRoles
                .Where(ar => ar.AdminUserId == userId && ar.Role.IsActive)
                .SelectMany(ar => ar.Role.RolePermissions.Select(rp => rp.Module))
                .Distinct()
                .ToListAsync();

            modules = [.. perms];
            cache.Set(cacheKey, modules, TimeSpan.FromMinutes(5));
        }

        return modules ?? [];
    }

    public IActionResult UserGuide()
    {
        ViewData["PageTitle"] = "User Guide";
        ViewData["PageSubtitle"] = "How to use the Payroll System";
        return View();
    }

    public IActionResult AccessDenied()
    {
        ViewData["PageTitle"] = "Access Denied";
        return View();
    }

    public IActionResult Privacy()
    {
        ViewData["PageTitle"] = "Privacy Policy";
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
