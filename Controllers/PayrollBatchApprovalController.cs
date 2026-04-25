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
[RequireModuleAccess(Modules.BatchPayrollApproval)]
public class PayrollBatchApprovalController(AppDbContext db, INotificationService notifications) : Controller
{
    private const int PageSize = 10;

    public async Task<IActionResult> Index(int page = 1, int? year = null, int? month = null)
    {
        ViewData["PageTitle"] = "Batch Approval";
        ViewData["PageSubtitle"] = "Review and approve submitted payroll batches";

        var query = db.PayrollBatches
            .Where(b => b.Status == PayrollBatchStatus.Submitted)
            .AsQueryable();

        if (year.HasValue)  query = query.Where(b => b.Year  == year.Value);
        if (month.HasValue) query = query.Where(b => b.Month == month.Value);

        query = query.OrderByDescending(b => b.SubmittedAt).ThenByDescending(b => b.Year).ThenByDescending(b => b.Month);

        var total   = await query.CountAsync();
        var batches = await query
            .Include(b => b.SubmittedByAdminUser)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var years = await db.PayrollBatches
            .Where(b => b.Status == PayrollBatchStatus.Submitted)
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
        ViewBag.Years      = years;

        return View(batches);
    }

    public async Task<IActionResult> Review(int id)
    {
        var batch = await db.PayrollBatches
            .Include(b => b.SubmittedByAdminUser)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (batch == null) return NotFound();

        if (batch.Status != PayrollBatchStatus.Submitted)
        {
            TempData["Error"] = "This batch is not pending approval.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["PageTitle"]    = "Review Batch";
        ViewData["PageSubtitle"] = $"Approve or reject the {new DateTime(batch.Year, batch.Month, 1):MMMM yyyy} payroll batch";

        return View(new BatchApprovalReviewViewModel
        {
            Batch               = batch,
            PayrollItems        = await LoadPayrollItems(id),
            SubmittedByUsername = batch.SubmittedByAdminUser?.Username,
            IsOwnSubmission     = batch.SubmittedByAdminUserId == GetUserId()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var batch = await db.PayrollBatches.FindAsync(id);
        if (batch == null) return NotFound();

        if (batch.Status != PayrollBatchStatus.Submitted)
        {
            TempData["Error"] = "Only submitted batches can be approved.";
            return RedirectToAction(nameof(Index));
        }

        if (batch.SubmittedByAdminUserId == GetUserId())
        {
            TempData["Error"] = "You cannot approve a batch that you submitted.";
            return RedirectToAction(nameof(Review), new { id });
        }

        batch.Status      = PayrollBatchStatus.Processing;
        batch.ProcessedAt = DateTime.UtcNow;

        QueueEvent(batch.Id, "Approved");

        await SyncPayrollStatuses(id, PayrollBatchStatus.Processing);
        await db.SaveChangesAsync();

        if (batch.SubmittedByAdminUserId.HasValue)
            await notifications.CreateAsync(
                batch.SubmittedByAdminUserId.Value,
                $"Batch {MonthName(batch.Month)} {batch.Year} Approved",
                $"Your payroll batch for {MonthName(batch.Month)} {batch.Year} has been approved and is now processing.",
                "success",
                Url.Action("Edit", "PayrollBatch", new { id = batch.Id }));

        TempData["Success"] = $"Batch for {MonthName(batch.Month)} {batch.Year} has been approved and is now processing.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(RejectBatchViewModel vm)
    {
        var batch = await db.PayrollBatches.FindAsync(vm.BatchId);
        if (batch == null) return NotFound();

        if (batch.Status != PayrollBatchStatus.Submitted)
        {
            TempData["Error"] = "Only submitted batches can be rejected.";
            return RedirectToAction(nameof(Index));
        }

        if (batch.SubmittedByAdminUserId == GetUserId())
        {
            TempData["Error"] = "You cannot reject a batch that you submitted.";
            return RedirectToAction(nameof(Review), new { id = vm.BatchId });
        }

        if (!ModelState.IsValid)
        {
            ViewData["PageTitle"]    = "Review Batch";
            ViewData["PageSubtitle"] = $"Approve or reject the {new DateTime(batch.Year, batch.Month, 1):MMMM yyyy} payroll batch";

            batch = await db.PayrollBatches
                .Include(b => b.SubmittedByAdminUser)
                .FirstAsync(b => b.Id == vm.BatchId);

            return View("Review", new BatchApprovalReviewViewModel
            {
                Batch               = batch,
                PayrollItems        = await LoadPayrollItems(vm.BatchId),
                SubmittedByUsername = batch.SubmittedByAdminUser?.Username
            });
        }

        batch.Status          = PayrollBatchStatus.Rejected;
        batch.RejectionReason = vm.Reason;

        QueueEvent(batch.Id, "Rejected", vm.Reason);

        await db.SaveChangesAsync();

        if (batch.SubmittedByAdminUserId.HasValue)
            await notifications.CreateAsync(
                batch.SubmittedByAdminUserId.Value,
                $"Batch {MonthName(batch.Month)} {batch.Year} Rejected",
                $"Your payroll batch for {MonthName(batch.Month)} {batch.Year} was rejected. Reason: {vm.Reason}",
                "danger",
                Url.Action("Edit", "PayrollBatch", new { id = batch.Id }));

        TempData["Info"] = $"Batch for {MonthName(batch.Month)} {batch.Year} has been rejected and returned to the submitter.";
        return RedirectToAction(nameof(Index));
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task SyncPayrollStatuses(int batchId, PayrollBatchStatus batchStatus)
    {
        var payrollStatus = batchStatus == PayrollBatchStatus.Processing
            ? PayrollStatus.Approved
            : PayrollStatus.Draft;

        var payrolls = await db.Payrolls
            .Where(p => p.PayrollBatchId == batchId)
            .ToListAsync();

        foreach (var p in payrolls)
            p.Status = payrollStatus;
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
