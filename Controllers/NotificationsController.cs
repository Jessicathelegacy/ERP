using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payroll.Services;

namespace Payroll.Controllers;

[Authorize]
public class NotificationsController(INotificationService notifications) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Dropdown()
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();

        var items = await notifications.GetRecentAsync(userId.Value);

        return Json(new
        {
            count = items.Count(n => !n.IsRead),
            items = items.Select(n => new
            {
                n.Id,
                n.Title,
                n.Message,
                n.Type,
                n.LinkUrl,
                n.IsRead,
                TimeAgo = FormatTimeAgo(n.CreatedAt)
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();

        await notifications.MarkAsReadAsync(id, userId.Value);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();

        await notifications.MarkAllAsReadAsync(userId.Value);
        return Ok();
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private int? CurrentUserId()
    {
        var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(s, out var id) ? id : null;
    }

    private static string FormatTimeAgo(DateTime utc)
    {
        var diff = DateTime.UtcNow - utc;
        if (diff.TotalMinutes < 1)  return "just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours   < 24) return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays    < 7)  return $"{(int)diff.TotalDays}d ago";
        return utc.ToLocalTime().ToString("MMM d");
    }
}
