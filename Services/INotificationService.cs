using Payroll.Models;

namespace Payroll.Services;

public interface INotificationService
{
    Task CreateAsync(int recipientId, string title, string message, string type = "info", string? linkUrl = null);
    Task CreateForModuleAsync(string module, string title, string message, string type = "info", string? linkUrl = null);
    Task<List<Notification>> GetRecentAsync(int userId, int count = 20);
    Task<int> GetUnreadCountAsync(int userId);
    Task MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
}
