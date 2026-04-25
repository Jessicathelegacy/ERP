using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Payroll.Data;
using Payroll.Filters;
using Payroll.Models;
using Payroll.ViewModels;

namespace Payroll.Controllers;

[Authorize]
[RequireModuleAccess(Modules.AdminUsers)]
public class AdminUserController(AppDbContext db) : Controller
{
    private const int PageSize = 10;

    public async Task<IActionResult> Index(int page = 1, string? search = null, string? status = null)
    {
        ViewData["PageTitle"] = "Admin Users";
        ViewData["PageSubtitle"] = "Manage administrator accounts";

        var query = db.AdminUsers
            .Include(a => a.AdminUserRoles).ThenInclude(ar => ar.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.Username.Contains(search));

        if (status == "active")   query = query.Where(a => a.IsActive);
        if (status == "inactive") query = query.Where(a => !a.IsActive);

        query = query.OrderBy(a => a.Username);

        var total = await query.CountAsync();
        var admins = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Page       = page;
        ViewBag.PageSize   = PageSize;
        ViewBag.TotalCount = total;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        ViewBag.Search     = search;
        ViewBag.Status     = status;

        return View(admins);
    }

    public async Task<IActionResult> Create()
    {
        ViewData["PageTitle"] = "Add Admin User";
        ViewData["PageSubtitle"] = "Register a new administrator account";
        await PopulateRoles();
        return View(new AdminUserCreateViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminUserCreateViewModel vm)
    {
        if (await db.AdminUsers.AnyAsync(a => a.Username == vm.Username))
            ModelState.AddModelError(nameof(vm.Username), "This username is already taken.");

        if (!ModelState.IsValid)
        {
            ViewData["PageTitle"] = "Add Admin User";
            ViewData["PageSubtitle"] = "Register a new administrator account";
            await PopulateRoles(vm.SelectedRoleIds);
            return View(vm);
        }

        var admin = new AdminUser
        {
            Username     = vm.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.Password),
            IsActive     = vm.IsActive,
            CreatedAt    = DateTime.UtcNow
        };

        db.AdminUsers.Add(admin);
        await db.SaveChangesAsync();

        await SyncRoles(admin.Id, vm.SelectedRoleIds);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Admin user \"{vm.Username}\" created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var admin = await db.AdminUsers
            .Include(a => a.AdminUserRoles)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (admin == null) return NotFound();

        ViewData["PageTitle"] = "Edit Admin User";
        ViewData["PageSubtitle"] = "Update administrator account";

        var selectedIds = admin.AdminUserRoles.Select(ar => ar.RoleId).ToList();
        await PopulateRoles(selectedIds);

        return View(new AdminUserEditViewModel
        {
            Id              = admin.Id,
            Username        = admin.Username,
            IsActive        = admin.IsActive,
            SelectedRoleIds = selectedIds
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminUserEditViewModel vm)
    {
        if (id != vm.Id) return BadRequest();

        if (await db.AdminUsers.AnyAsync(a => a.Username == vm.Username && a.Id != id))
            ModelState.AddModelError(nameof(vm.Username), "This username is already taken.");

        if (!ModelState.IsValid)
        {
            ViewData["PageTitle"] = "Edit Admin User";
            ViewData["PageSubtitle"] = "Update administrator account";
            await PopulateRoles(vm.SelectedRoleIds);
            return View(vm);
        }

        var admin = await db.AdminUsers
            .Include(a => a.AdminUserRoles)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (admin == null) return NotFound();

        admin.Username = vm.Username;
        admin.IsActive = vm.IsActive;

        await SyncRoles(admin.Id, vm.SelectedRoleIds, admin.AdminUserRoles);
        await db.SaveChangesAsync();

        TempData["Success"] = $"Admin user \"{admin.Username}\" updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> ChangePassword(int id)
    {
        var admin = await db.AdminUsers.FindAsync(id);
        if (admin == null) return NotFound();

        ViewData["PageTitle"] = "Change Password";
        ViewData["PageSubtitle"] = $"Reset password for {admin.Username}";

        return View(new ChangePasswordViewModel
        {
            Id       = admin.Id,
            Username = admin.Username
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(int id, ChangePasswordViewModel vm)
    {
        if (id != vm.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            ViewData["PageTitle"] = "Change Password";
            ViewData["PageSubtitle"] = $"Reset password for {vm.Username}";
            return View(vm);
        }

        var admin = await db.AdminUsers.FindAsync(id);
        if (admin == null) return NotFound();

        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.NewPassword);

        await db.SaveChangesAsync();
        TempData["Success"] = $"Password for \"{admin.Username}\" changed successfully.";
        return RedirectToAction(nameof(Index));
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task PopulateRoles(List<int>? selectedIds = null)
    {
        ViewBag.AllRoles  = await db.Roles.Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync();
        ViewBag.SelectedRoleIds = selectedIds ?? [];
    }

    private async Task SyncRoles(int adminId, List<int> selectedIds,
        ICollection<AdminUserRole>? existing = null)
    {
        existing ??= await db.AdminUserRoles.Where(ar => ar.AdminUserId == adminId).ToListAsync();

        var toRemove = existing.Where(ar => !selectedIds.Contains(ar.RoleId)).ToList();
        var toAdd    = selectedIds
            .Where(rid => !existing.Any(ar => ar.RoleId == rid))
            .Select(rid => new AdminUserRole { AdminUserId = adminId, RoleId = rid });

        db.AdminUserRoles.RemoveRange(toRemove);
        db.AdminUserRoles.AddRange(toAdd);
    }
}
