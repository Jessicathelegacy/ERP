using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payroll.Data;
using Payroll.Filters;
using Payroll.Models;
using Payroll.Services;
using Payroll.ViewModels;

namespace Payroll.Controllers;

[Authorize]
[RequireModuleAccess(Modules.BatchPayroll)]
public class PayrollBatchController(AppDbContext db, INotificationService notifications) : Controller
{
    private const int PageSize = 10;

    public async Task<IActionResult> Index(int page = 1, int? year = null, int? month = null, string? status = null)
    {
        ViewData["PageTitle"] = "Batch Payroll";
        ViewData["PageSubtitle"] = "Manage monthly payroll batches";

        var query = db.PayrollBatches.AsQueryable();

        if (year.HasValue)  query = query.Where(b => b.Year  == year.Value);
        if (month.HasValue) query = query.Where(b => b.Month == month.Value);

        if (status == "pending")    query = query.Where(b => b.Status == PayrollBatchStatus.Draft || b.Status == PayrollBatchStatus.Processing);
        if (status == "draft")      query = query.Where(b => b.Status == PayrollBatchStatus.Draft);
        if (status == "submitted")  query = query.Where(b => b.Status == PayrollBatchStatus.Submitted);
        if (status == "rejected")   query = query.Where(b => b.Status == PayrollBatchStatus.Rejected);
        if (status == "processing") query = query.Where(b => b.Status == PayrollBatchStatus.Processing);
        if (status == "completed")  query = query.Where(b => b.Status == PayrollBatchStatus.Completed);
        if (status == "cancelled")  query = query.Where(b => b.Status == PayrollBatchStatus.Cancelled);

        query = query.OrderByDescending(b => b.Year).ThenByDescending(b => b.Month);

        var total = await query.CountAsync();
        var batches = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var years = await db.PayrollBatches
            .Select(b => b.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();

        ViewBag.Page       = page;
        ViewBag.PageSize   = PageSize;
        ViewBag.TotalCount = total;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        ViewBag.Year       = year;
        ViewBag.Month      = month;
        ViewBag.Status     = status;
        ViewBag.Years      = years;

        return View(batches);
    }

    public IActionResult Create()
    {
        ViewData["PageTitle"] = "Create Batch";
        ViewData["PageSubtitle"] = "Set up a new payroll batch";

        return View(new PayrollBatch
        {
            Year = DateTime.Today.Year,
            Month = DateTime.Today.Month,
            Status = PayrollBatchStatus.Draft,
            CreatedAt = DateTime.UtcNow
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PayrollBatch model, string? action)
    {
        if (await db.PayrollBatches.AnyAsync(b => b.Year == model.Year && b.Month == model.Month))
            ModelState.AddModelError("Month", $"A batch for {MonthName(model.Month)} {model.Year} already exists.");

        if (!ModelState.IsValid)
        {
            ViewData["PageTitle"] = "Create Batch";
            ViewData["PageSubtitle"] = "Set up a new payroll batch";
            return View(model);
        }

        model.CreatedAt = DateTime.UtcNow;
        model.Status    = action == "submit" ? PayrollBatchStatus.Submitted : PayrollBatchStatus.Draft;

        if (action == "submit")
        {
            var userId = GetUserId();
            if (userId == null) return Forbid();

            model.SubmittedByAdminUserId = userId.Value;
            model.SubmittedAt            = DateTime.UtcNow;
        }

        db.PayrollBatches.Add(model);
        await db.SaveChangesAsync();

        QueueEvent(model.Id, action == "submit" ? "Submitted" : "Drafted");
        await db.SaveChangesAsync();

        await GeneratePayrollsForBatch(model);

        if (action == "submit")
        {
            await notifications.CreateForModuleAsync(
                Modules.BatchPayrollApproval,
                $"Batch {MonthName(model.Month)} {model.Year} Submitted",
                $"A payroll batch for {MonthName(model.Month)} {model.Year} has been submitted and requires your approval.",
                "warning",
                Url.Action("Index", "PayrollBatchApproval"));

            TempData["Success"] = $"Batch for {MonthName(model.Month)} {model.Year} created and submitted for approval with {model.TotalEmployees} employee records.";
        }
        else
        {
            TempData["Success"] = $"Batch for {MonthName(model.Month)} {model.Year} saved as draft with {model.TotalEmployees} employee records.";
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var batch = await db.PayrollBatches.FindAsync(id);
        if (batch == null) return NotFound();

        ViewData["PageTitle"] = "Edit Batch";
        ViewData["PageSubtitle"] = "Update payroll batch details";

        return View(new PayrollBatchEditViewModel
        {
            Batch        = batch,
            PayrollItems = await LoadPayrollItems(id),
            Events       = await db.PayrollBatchEvents
                               .Include(e => e.AdminUser)
                               .Where(e => e.PayrollBatchId == id)
                               .OrderByDescending(e => e.OccurredAt)
                               .ToListAsync()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PayrollBatchEditViewModel vm)
    {
        var batch = await db.PayrollBatches.FindAsync(id);
        if (batch == null) return NotFound();

        if (batch.Status == PayrollBatchStatus.Submitted || batch.Status == PayrollBatchStatus.Processing)
        {
            TempData["Error"] = "This batch cannot be edited in its current state.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var model = vm.Batch;
        if (id != model.Id) return BadRequest();

        if (await db.PayrollBatches.AnyAsync(b => b.Year == model.Year && b.Month == model.Month && b.Id != id))
            ModelState.AddModelError("Batch.Month", $"A batch for {MonthName(model.Month)} {model.Year} already exists.");

        if (!ModelState.IsValid)
        {
            ViewData["PageTitle"] = "Edit Batch";
            ViewData["PageSubtitle"] = "Update payroll batch details";
            vm.PayrollItems = await LoadPayrollItems(id);
            return View(vm);
        }

        var previousStatus = batch.Status;

        batch.Year        = model.Year;
        batch.Month       = model.Month;
        batch.Description = model.Description;
        batch.Status      = model.Status;

        if (previousStatus != PayrollBatchStatus.Completed && batch.Status == PayrollBatchStatus.Completed)
            batch.ProcessedAt = DateTime.UtcNow;

        if (previousStatus != batch.Status)
            await SyncPayrollStatuses(id, batch.Status);

        await RecalculateTotals(batch);

        QueueEvent(batch.Id, "Updated");
        await db.SaveChangesAsync();
        TempData["Success"] = $"Batch for {MonthName(batch.Month)} {batch.Year} updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id)
    {
        var batch = await db.PayrollBatches.FindAsync(id);
        if (batch == null) return NotFound();

        if (batch.Status != PayrollBatchStatus.Draft && batch.Status != PayrollBatchStatus.Rejected)
        {
            TempData["Error"] = "Only Draft or Rejected batches can be submitted for approval.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId)) return Forbid();

        batch.Status                 = PayrollBatchStatus.Submitted;
        batch.SubmittedByAdminUserId = userId;
        batch.SubmittedAt            = DateTime.UtcNow;
        batch.RejectionReason        = null;

        QueueEvent(batch.Id, "Submitted");

        await db.SaveChangesAsync();

        await notifications.CreateForModuleAsync(
            Modules.BatchPayrollApproval,
            $"Batch {MonthName(batch.Month)} {batch.Year} Submitted",
            $"A payroll batch for {MonthName(batch.Month)} {batch.Year} has been submitted and requires your approval.",
            "warning",
            Url.Action("Index", "PayrollBatchApproval"));

        TempData["Success"] = $"Batch for {MonthName(batch.Month)} {batch.Year} has been submitted for approval.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Recall(int id)
    {
        var batch = await db.PayrollBatches.FindAsync(id);
        if (batch == null) return NotFound();

        if (batch.Status != PayrollBatchStatus.Submitted)
        {
            TempData["Error"] = "Only Submitted batches can be recalled.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        batch.Status                 = PayrollBatchStatus.Draft;
        batch.SubmittedAt            = null;
        batch.SubmittedByAdminUserId = null;

        QueueEvent(batch.Id, "Recalled");

        await db.SaveChangesAsync();

        TempData["Success"] = $"Batch for {MonthName(batch.Month)} {batch.Year} has been recalled and returned to Draft.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var batch = await db.PayrollBatches.FindAsync(id);
        if (batch == null) return NotFound();

        if (batch.Status != PayrollBatchStatus.Processing)
        {
            TempData["Error"] = "Only Processing batches can be marked as Completed.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        batch.Status      = PayrollBatchStatus.Completed;
        batch.ProcessedAt = DateTime.UtcNow;

        await SyncPayrollStatuses(id, PayrollBatchStatus.Completed);
        QueueEvent(batch.Id, "Completed");

        await db.SaveChangesAsync();

        TempData["Success"] = $"Batch for {MonthName(batch.Month)} {batch.Year} has been marked as Completed.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelBatch(int id)
    {
        var batch = await db.PayrollBatches.FindAsync(id);
        if (batch == null) return NotFound();

        if (batch.Status != PayrollBatchStatus.Processing)
        {
            TempData["Error"] = "Only Processing batches can be cancelled from this action.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        batch.Status = PayrollBatchStatus.Cancelled;

        await SyncPayrollStatuses(id, PayrollBatchStatus.Cancelled);
        QueueEvent(batch.Id, "Cancelled");

        await db.SaveChangesAsync();

        TempData["Success"] = $"Batch for {MonthName(batch.Month)} {batch.Year} has been cancelled.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDeductions(UpdateDeductionsViewModel vm)
    {
        var batchCheck = await db.PayrollBatches.FindAsync(vm.BatchId);
        if (batchCheck?.Status == PayrollBatchStatus.Submitted)
        {
            TempData["Error"] = "This batch is under review and cannot be edited.";
            return RedirectToAction(nameof(Edit), new { id = vm.BatchId });
        }

        var payrolls = await db.Payrolls
            .Where(p => p.PayrollBatchId == vm.BatchId)
            .ToListAsync();

        foreach (var item in vm.Items)
        {
            var payroll = payrolls.FirstOrDefault(p => p.Id == item.PayrollId);
            if (payroll == null) continue;

            var otPay = Math.Round(item.OvertimeHours * item.OvertimeRatePerHour, 2);
            payroll.OvertimeHours = item.OvertimeHours;
            payroll.OvertimePay   = otPay;
            payroll.GrossSalary   = payroll.BasicSalary + otPay + payroll.Allowances;
            payroll.Deductions    = item.Deductions;
            payroll.NetSalary     = payroll.GrossSalary - item.Deductions;
        }

        var batch = await db.PayrollBatches.FindAsync(vm.BatchId);
        if (batch != null)
            await RecalculateTotals(batch);

        await db.SaveChangesAsync();
        TempData["Success"]   = "Overtime and deductions saved successfully.";
        TempData["ActiveTab"] = "payrolls";
        return RedirectToAction(nameof(Edit), new { id = vm.BatchId });
    }

    public async Task<IActionResult> GetPeriodTotals(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end   = start.AddMonths(1).AddDays(-1);

        var rows = await db.Payrolls
            .Where(p => p.PayPeriodStart >= start && p.PayPeriodEnd <= end)
            .ToListAsync();

        return Json(new
        {
            totalEmployees   = rows.Select(p => p.EmployeeId).Distinct().Count(),
            totalGrossSalary = rows.Sum(p => p.GrossSalary),
            totalDeductions  = rows.Sum(p => p.Deductions),
            totalNetSalary   = rows.Sum(p => p.NetSalary)
        });
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task GeneratePayrollsForBatch(PayrollBatch batch)
    {
        var periodStart = new DateTime(batch.Year, batch.Month, 1);
        var periodEnd   = periodStart.AddMonths(1).AddDays(-1);
        var payrollStatus = MapToPayrollStatus(batch.Status);

        var employees = await db.Employees
            .Include(e => e.PaymentScheme)
            .Where(e => e.IsActive)
            .OrderBy(e => e.Name)
            .ToListAsync();

        foreach (var employee in employees)
        {
            var gross = employee.PaymentScheme.BasicSalary + employee.PaymentScheme.AllowanceAmount;
            db.Payrolls.Add(new Models.Payroll
            {
                EmployeeId     = employee.Id,
                PayrollBatchId = batch.Id,
                PayPeriodStart = periodStart,
                PayPeriodEnd   = periodEnd,
                BasicSalary    = employee.PaymentScheme.BasicSalary,
                OvertimeHours  = 0,
                OvertimePay    = 0,
                Allowances     = employee.PaymentScheme.AllowanceAmount,
                Deductions     = 0,
                GrossSalary    = gross,
                NetSalary      = gross,
                Status         = payrollStatus,
                CreatedAt      = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();

        await RecalculateTotals(batch);
        await db.SaveChangesAsync();
    }

    private async Task SyncPayrollStatuses(int batchId, PayrollBatchStatus batchStatus)
    {
        var payrollStatus = MapToPayrollStatus(batchStatus);
        var payrolls = await db.Payrolls
            .Where(p => p.PayrollBatchId == batchId)
            .ToListAsync();

        foreach (var p in payrolls)
            p.Status = payrollStatus;
    }

    private async Task RecalculateTotals(PayrollBatch batch)
    {
        var payrolls = await db.Payrolls
            .Where(p => p.PayrollBatchId == batch.Id)
            .ToListAsync();

        batch.TotalEmployees   = payrolls.Select(p => p.EmployeeId).Distinct().Count();
        batch.TotalGrossSalary = payrolls.Sum(p => p.GrossSalary);
        batch.TotalDeductions  = payrolls.Sum(p => p.Deductions);
        batch.TotalNetSalary   = payrolls.Sum(p => p.NetSalary);
    }

    private async Task<List<BatchPayrollItem>> LoadPayrollItems(int batchId) =>
        await db.Payrolls
            .Include(p => p.Employee).ThenInclude(e => e.PaymentScheme)
            .Where(p => p.PayrollBatchId == batchId)
            .OrderBy(p => p.Employee.Name)
            .Select(p => new BatchPayrollItem
            {
                Id                  = p.Id,
                EmployeeName        = p.Employee.Name,
                BasicSalary         = p.BasicSalary,
                OvertimeHours       = p.OvertimeHours,
                OvertimeRatePerHour = p.Employee.PaymentScheme.OvertimeRatePerHour,
                OvertimePay         = p.OvertimePay,
                Allowances          = p.Allowances,
                Deductions          = p.Deductions,
                GrossSalary         = p.GrossSalary,
                NetSalary           = p.NetSalary,
                Status              = p.Status
            })
            .ToListAsync();

    private static PayrollStatus MapToPayrollStatus(PayrollBatchStatus batchStatus) =>
        batchStatus switch
        {
            PayrollBatchStatus.Processing => PayrollStatus.Approved,
            PayrollBatchStatus.Completed  => PayrollStatus.Paid,
            _                             => PayrollStatus.Draft   // Draft, Submitted, Rejected, Cancelled
        };

    private void QueueEvent(int batchId, string eventType, string? note = null) =>
        db.PayrollBatchEvents.Add(new PayrollBatchEvent
        {
            PayrollBatchId = batchId,
            EventType      = eventType,
            AdminUserId    = GetUserId(),
            Note           = note,
            OccurredAt     = DateTime.UtcNow
        });

    private int? GetUserId()
    {
        var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(s, out var id) ? id : null;
    }

    private static string MonthName(int month) =>
        new DateTime(2000, month, 1).ToString("MMMM");
}
