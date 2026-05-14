using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models;
using System.Security.Claims;

namespace PropertyManagement.API.Controllers
{
    // serves in-system notifications for all roles
    // every user can only read and manage their own notifications
    // notifications are created by NotificationService when business events occur
    // such as lease approvals maintenance assignments and payment receipts
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET api/Notifications - returns all notifications for the current user newest first
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedDate)
                .Select(n => new
                {
                    n.NotificationId,
                    n.Message,
                    n.Type,
                    n.IsRead,
                    n.CreatedDate
                })
                .ToListAsync();

            return Ok(notifications);
        }

        // GET api/Notifications/unread-count - returns just the unread count for the navbar badge
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var count = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return Ok(new { unreadCount = count });
        }

        // PUT api/Notifications/5/read - marks a single notification as read
        // only the owner of the notification is allowed to do this
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId       = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null)
                return NotFound(new { message = "Notification not found" });

            if (notification.UserId != userId)
                return Forbid();

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notification marked as read" });
        }

        // PUT api/Notifications/mark-all-read - marks all unread notifications as read in one call
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            unread.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"{unread.Count} notifications marked as read" });
        }

        // DELETE api/Notifications/5 - permanently removes a notification
        // only the owner of the notification is allowed to delete it
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId       = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null)
                return NotFound(new { message = "Notification not found" });

            if (notification.UserId != userId)
                return Forbid();

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notification deleted" });
        }
    }
}
