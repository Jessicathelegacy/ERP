using Microsoft.EntityFrameworkCore;
using Payroll.Data;
using Payroll.Models;

namespace Payroll.Services;

public class NotificationService(AppDbContext db) : INotificationService
{
    public async Task CreateAsync(int recipientId, string title, string message, string type = "info", string? linkUrl = null)
    {
        db.Notifications.Add(new Notification
        {
            RecipientAdminUserId = recipientId,
            Title    = title,
            Message  = message,
            Type     = type,
            LinkUrl  = linkUrl,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    public async Task CreateForModuleAsync(string module, string title, string message, string type = "info", string? linkUrl = null)
    {
        var recipientIds = await db.AdminUsers
            .Where(u => u.IsActive && u.AdminUserRoles.Any(ar =>
                ar.Role.RolePermissions.Any(rp => rp.Module == module)))
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var id in recipientIds)
        {
            db.Notifications.Add(new Notification
            {
                RecipientAdminUserId = id,
                Title    = title,
                Message  = message,
                Type     = type,
                LinkUrl  = linkUrl,
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }

    public Task<List<Notification>> GetRecentAsync(int userId, int count = 20) =>
        db.Notifications
            .Where(n => n.RecipientAdminUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .ToListAsync();

    public Task<int> GetUnreadCountAsync(int userId) =>
        db.Notifications.CountAsync(n => n.RecipientAdminUserId == userId && !n.IsRead);

    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        var n = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientAdminUserId == userId);
        if (n is { IsRead: false })
        {
            n.IsRead = true;
            await db.SaveChangesAsync();
        }
    }

    public Task MarkAllAsReadAsync(int userId) =>
        db.Notifications
            .Where(n => n.RecipientAdminUserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
}
