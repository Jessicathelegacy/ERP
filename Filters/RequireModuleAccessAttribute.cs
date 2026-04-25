using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Payroll.Data;

namespace Payroll.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireModuleAccessAttribute(string module) : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
            return;

        var db    = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();

        var userIdStr = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId)) { Deny(context); return; }

        var version  = cache.Get<long>("perm_version");
        var cacheKey = $"perm:{userId}:{module}:{version}";

        if (!cache.TryGetValue(cacheKey, out bool hasPermission))
        {
            hasPermission = await db.AdminUserRoles
                .Where(ar => ar.AdminUserId == userId)
                .AnyAsync(ar => ar.Role.IsActive &&
                                ar.Role.RolePermissions.Any(rp => rp.Module == module));

            cache.Set(cacheKey, hasPermission, TimeSpan.FromMinutes(5));
        }

        if (!hasPermission) Deny(context);
    }

    private static void Deny(AuthorizationFilterContext context) =>
        context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
}
