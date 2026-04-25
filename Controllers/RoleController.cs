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
[RequireModuleAccess(Modules.Roles)]
public class RoleController(AppDbContext db, IMemoryCache cache) : Controller
{
    private const int PageSize = 10;

    public async Task<IActionResult> Index(int page = 1, string? search = null, string? status = null)
    {
        ViewData["PageTitle"] = "Roles";
        ViewData["PageSubtitle"] = "Manage administrator roles";

        var query = db.Roles
            .Select(r => new RoleListViewModel
            {
                Id          = r.Id,
                Name        = r.Name,
                Description = r.Description,
                IsActive    = r.IsActive,
                UserCount   = r.AdminUserRoles.Count
            })
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.Name.Contains(search) || (r.Description != null && r.Description.Contains(search)));

        if (status == "active")   query = query.Where(r => r.IsActive);
        if (status == "inactive") query = query.Where(r => !r.IsActive);

        query = query.OrderBy(r => r.Name);

        var total = await query.CountAsync();
        var roles = await query
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewBag.Page       = page;
        ViewBag.PageSize   = PageSize;
        ViewBag.TotalCount = total;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        ViewBag.Search     = search;
        ViewBag.Status     = status;

        return View(roles);
    }

    public IActionResult Create()
    {
        ViewData["PageTitle"] = "Add Role";
        ViewData["PageSubtitle"] = "Create a new administrator role";
        return View(new RoleFormViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleFormViewModel vm)
    {
        if (await db.Roles.AnyAsync(r => r.Name == vm.Name))
            ModelState.AddModelError(nameof(vm.Name), "A role with this name already exists.");

        if (!ModelState.IsValid)
        {
            ViewData["PageTitle"] = "Add Role";
            ViewData["PageSubtitle"] = "Create a new administrator role";
            return View(vm);
        }

        var role = new Role
        {
            Name        = vm.Name,
            Description = vm.Description,
            IsActive    = vm.IsActive
        };

        var desired = vm.GrantedModules.Where(m => Modules.All.Any(a => a.Key == m)).ToHashSet();
        role.RolePermissions = desired.Select(m => new RolePermission { Module = m }).ToList();

        db.Roles.Add(role);
        await db.SaveChangesAsync();

        cache.Set("perm_version", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        TempData["Success"] = $"Role \"{vm.Name}\" created successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var role = await db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return NotFound();

        ViewData["PageTitle"] = "Edit Role";
        ViewData["PageSubtitle"] = "Update role details";

        return View(new RoleFormViewModel
        {
            Id             = role.Id,
            Name           = role.Name,
            Description    = role.Description,
            IsActive       = role.IsActive,
            GrantedModules = role.RolePermissions.Select(rp => rp.Module).ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RoleFormViewModel vm)
    {
        if (id != vm.Id) return BadRequest();

        if (await db.Roles.AnyAsync(r => r.Name == vm.Name && r.Id != id))
            ModelState.AddModelError(nameof(vm.Name), "A role with this name already exists.");

        if (!ModelState.IsValid)
        {
            ViewData["PageTitle"] = "Edit Role";
            ViewData["PageSubtitle"] = "Update role details";
            return View(vm);
        }

        var role = await db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return NotFound();

        role.Name        = vm.Name;
        role.Description = vm.Description;
        role.IsActive    = vm.IsActive;

        var desired  = vm.GrantedModules.Where(m => Modules.All.Any(a => a.Key == m)).ToHashSet();
        var existing = role.RolePermissions.Select(rp => rp.Module).ToHashSet();

        foreach (var m in existing.Except(desired))
            role.RolePermissions.Remove(role.RolePermissions.First(rp => rp.Module == m));

        foreach (var m in desired.Except(existing))
            role.RolePermissions.Add(new RolePermission { RoleId = id, Module = m });

        await db.SaveChangesAsync();

        cache.Set("perm_version", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        TempData["Success"] = $"Role \"{role.Name}\" updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Permissions(int id)
    {
        var role = await db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return NotFound();

        ViewData["PageTitle"]    = "Module Permissions";
        ViewData["PageSubtitle"] = $"Permissions for {role.Name}";

        ViewBag.Granted = new HashSet<string>(role.RolePermissions.Select(rp => rp.Module));
        return View(role);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Permissions(int id, List<string> modules)
    {
        var role = await db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (role == null) return NotFound();

        var desired  = modules.Where(m => Modules.All.Any(a => a.Key == m)).ToHashSet();
        var existing = role.RolePermissions.Select(rp => rp.Module).ToHashSet();

        foreach (var m in existing.Except(desired))
            role.RolePermissions.Remove(role.RolePermissions.First(rp => rp.Module == m));

        foreach (var m in desired.Except(existing))
            role.RolePermissions.Add(new RolePermission { RoleId = id, Module = m });

        await db.SaveChangesAsync();

        cache.Set("perm_version", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        TempData["Success"] = $"Permissions for \"{role.Name}\" saved.";
        return RedirectToAction(nameof(Index));
    }
}
