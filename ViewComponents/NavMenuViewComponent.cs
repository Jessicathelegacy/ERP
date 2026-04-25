using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Payroll.Data;

namespace Payroll.ViewComponents;

public class NavMenuViewComponent(AppDbContext db, IMemoryCache cache) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (UserClaimsPrincipal.Identity?.IsAuthenticated != true)
            return View(new HashSet<string>());

        var userIdStr = UserClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
            return View(new HashSet<string>());

        var version  = cache.Get<long>("perm_version");
        var cacheKey = $"nav_modules:{userId}:{version}";

        if (!cache.TryGetValue(cacheKey, out HashSet<string>? modules))
        {
            var perms = await db.AdminUserRoles
                .Where(ar => ar.AdminUserId == userId && ar.Role.IsActive)
                .SelectMany(ar => ar.Role.RolePermissions.Select(rp => rp.Module))
                .Distinct()
                .ToListAsync();

            modules = new HashSet<string>(perms);
            cache.Set(cacheKey, modules, TimeSpan.FromMinutes(5));
        }

        return View(modules ?? []);
    }
}
