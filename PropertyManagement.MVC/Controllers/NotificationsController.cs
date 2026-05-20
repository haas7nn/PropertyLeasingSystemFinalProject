using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.MVC.Models;
using System.Security.Claims;

namespace PropertyManagement.MVC.Controllers
{
    /// <summary>
    /// Displays in-system notifications for all authenticated users.
    /// Notifications are created by NotificationService when business events fire.
    /// </summary>
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /Notifications — list all notifications for the logged-in user
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .Select(n => new NotificationViewModel
                {
                    NotificationId = n.NotificationId,
                    Message        = n.Message,
                    Type           = n.Type,
                    IsRead         = n.IsRead,
                    CreatedDate    = n.CreatedDate
                })
                .ToListAsync();

            // Mark all as read when the page is opened
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (unread.Count > 0)
            {
                unread.ForEach(n => n.IsRead = true);
                await _context.SaveChangesAsync();
            }

            return View(notifications);
        }

        // POST /Notifications/Delete/5 — delete a single notification
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId       = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null || notification.UserId != userId)
                return NotFound();

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Notification deleted.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Notifications/DeleteAll — clear all notifications for the current user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var all = await _context.Notifications
                .Where(n => n.UserId == userId)
                .ToListAsync();

            _context.Notifications.RemoveRange(all);
            await _context.SaveChangesAsync();

            TempData["Success"] = "All notifications cleared.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Notifications/UnreadCount — JSON endpoint used by the navbar badge via AJAX
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Json(new { count = 0 });

            var count = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return Json(new { count });
        }
    }
}
