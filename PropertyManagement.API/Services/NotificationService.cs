using PropertyManagement.API.Data;
using PropertyManagement.API.Models;

namespace PropertyManagement.API.Services
{
    // creates and persists in-system notifications whenever a business event occurs
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        // base method that all the specific lifecycle methods call to save a notification
        public async Task CreateAsync(string userId, string message, string type)
        {
            var notification = new Notification
            {
                UserId      = userId,
                Message     = message,
                Type        = type,
                IsRead      = false,
                CreatedDate = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        // sent to the tenant when the property manager approves their lease application
        public Task LeaseApprovedAsync(string tenantId, string unitNumber)
            => CreateAsync(tenantId,
                $"Congratulations! Your lease application for Unit {unitNumber} has been approved. Please contact the office to complete move-in arrangements.",
                "LeaseApproved");

        // sent to the tenant when the property manager rejects their application
        public Task LeaseRejectedAsync(string tenantId, string unitNumber, string reason)
            => CreateAsync(tenantId,
                $"Your lease application for Unit {unitNumber} was not approved. Reason: {reason}. Please contact the office if you have questions.",
                "LeaseRejected");

        // sent to the tenant when the property manager terminates their active lease
        public Task LeaseTerminatedAsync(string tenantId, string unitNumber)
            => CreateAsync(tenantId,
                $"Your lease for Unit {unitNumber} has been terminated. Please contact the office regarding move-out arrangements and deposit return.",
                "LeaseTerminated");

        // sent to the tenant when the property manager extends their lease end date
        public Task LeaseRenewedAsync(string tenantId, string unitNumber, DateTime newEndDate)
            => CreateAsync(tenantId,
                $"Great news! Your lease for Unit {unitNumber} has been renewed until {newEndDate:dd MMM yyyy}.",
                "LeaseRenewed");

        // sent to the tenant when the manager records a payment receipt for their lease
        public Task PaymentRecordedAsync(string tenantId, decimal amountPaid)
            => CreateAsync(tenantId,
                $"Your payment of BD {amountPaid:N2} has been received and recorded. Thank you.",
                "PaymentRecorded");

        // sent to the tenant when one of their payments becomes overdue
        public Task PaymentOverdueAsync(string tenantId, decimal amount)
            => CreateAsync(tenantId,
                $"You have an overdue rent payment of BD {amount:N2}. Please contact the office to avoid penalties.",
                "PaymentOverdue");

        // sent to the property manager when a tenant submits a new maintenance request via MVC
        public Task MaintenanceSubmittedAsync(string managerId, string ticketNumber)
            => CreateAsync(managerId,
                $"A new maintenance request has been submitted by a tenant: {ticketNumber}. Please review and assign.",
                "MaintenanceSubmitted");

        // sent to the assigned staff member when a request is assigned to them
        public Task MaintenanceAssignedAsync(string staffId, string ticketNumber, string category)
            => CreateAsync(staffId,
                $"You have been assigned a {category} maintenance request: {ticketNumber}. Please review and get in touch with the tenant.",
                "MaintenanceAssigned");

        // sent to the tenant when their maintenance request has been marked as resolved
        public Task MaintenanceResolvedAsync(string tenantId, string ticketNumber)
            => CreateAsync(tenantId,
                $"Your maintenance request {ticketNumber} has been marked as Resolved. Please confirm the issue is fixed.",
                "MaintenanceResolved");

        // sent to staff when a new request matching their skill area is submitted via the API
        public Task MaintenanceNewRequestAsync(string staffId, string ticketNumber, string category)
            => CreateAsync(staffId,
                $"A new {category} maintenance request has been submitted: {ticketNumber}.",
                "NewMaintenanceRequest");
    }
}
