using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models;
using PropertyManagement.API.Services;
using PropertyManagement.MVC.Models;

namespace PropertyManagement.MVC.Controllers
{
    // both the property manager and tenants use this controller
    // tenants can view their own payments and managers can create and record them
    [Authorize]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        // used to notify tenants when the manager records a payment for their lease
        private readonly NotificationService _notificationService;

        // used to check who is logged in and what role they have
        private readonly UserManager<IdentityUser> _userManager;

        public PaymentsController(
            ApplicationDbContext context,
            NotificationService notificationService,
            UserManager<IdentityUser> userManager)
        {
            _context             = context;
            _notificationService = notificationService;
            _userManager         = userManager;
        }

        // shows the list of payments with optional status filtering
        public async Task<IActionResult> Index(string? status)
        {
            ViewBag.Status = status;

            var user     = await _userManager.GetUserAsync(User);
            var roles    = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            var isTenant = roles.Contains("Tenant");

            var query = _context.Payments
                .Include(p => p.Lease).ThenInclude(l => l.Tenant)
                .Include(p => p.Lease).ThenInclude(l => l.Unit)
                .AsQueryable();

            // apply the status filter if one was selected in the filter pills
            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            // tenants can only see payments for their own leases
            // we do this in the query so they cannot get around it by changing the URL
            if (isTenant && user != null)
                query = query.Where(p => p.Lease.TenantId == user.Id);

            // this sends a single SQL UPDATE to mark overdue payments before we read them
            // doing it in memory and then saving would cause EF to cache the old status
            // and show Pending instead of Overdue in the same request
            await _context.Payments
                .Where(p => p.Status == "Pending" && p.DueDate < DateTime.Now)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, "Overdue"));

            var payments = await query
                .OrderByDescending(p => p.DueDate)
                .Select(p => new PaymentViewModel
                {
                    PaymentId     = p.PaymentId,
                    LeaseId       = p.LeaseId,
                    TenantEmail   = p.Lease.Tenant.Email ?? "",
                    UnitNumber    = p.Lease.Unit.UnitNumber,
                    DueDate       = p.DueDate,
                    AmountDue     = p.AmountDue,
                    AmountPaid    = p.AmountPaid,
                    PaymentDate   = p.PaymentDate,
                    Status        = p.Status,
                    PaymentMethod = p.PaymentMethod,
                    ReceiptNumber = p.ReceiptNumber
                })
                .ToListAsync();

            return View(payments);
        }

        // shows the full details of one payment record
        public async Task<IActionResult> Details(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Lease).ThenInclude(l => l.Tenant)
                .Include(p => p.Lease).ThenInclude(l => l.Unit)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null) return NotFound();

            // tenants should only be able to see their own payment details
            // we use Forbid instead of NotFound so they know the record exists
            // just that they are not allowed to view it
            var currentUser = await _userManager.GetUserAsync(User);
            if (User.IsInRole("Tenant") && payment.Lease.TenantId != currentUser?.Id)
                return Forbid();

            return View(new PaymentViewModel
            {
                PaymentId     = payment.PaymentId,
                LeaseId       = payment.LeaseId,
                TenantEmail   = payment.Lease.Tenant.Email ?? "",
                UnitNumber    = payment.Lease.Unit.UnitNumber,
                DueDate       = payment.DueDate,
                AmountDue     = payment.AmountDue,
                AmountPaid    = payment.AmountPaid,
                PaymentDate   = payment.PaymentDate,
                Status        = payment.Status,
                PaymentMethod = payment.PaymentMethod,
                ReceiptNumber = payment.ReceiptNumber
            });
        }

        // shows the create form with only active leases in the dropdown
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Create()
        {
            // we only show active leases because you cannot create a payment
            // for a lease that has not been approved yet or one that has ended
            ViewBag.ActiveLeases = await _context.Leases
                .Include(l => l.Tenant)
                .Include(l => l.Unit)
                .Where(l => l.Status == "Active")
                .ToListAsync();

            return View(new PaymentViewModel { DueDate = DateTime.Now });
        }

        // saves a new payment record for an active lease
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Create(PaymentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // we reload the dropdown before going back so the form still works
                ViewBag.ActiveLeases = await _context.Leases
                    .Include(l => l.Tenant).Include(l => l.Unit)
                    .Where(l => l.Status == "Active").ToListAsync();
                return View(model);
            }

            // check the lease is still active at the moment of submitting
            // the manager might have terminated it between opening the form and submitting it
            var lease = await _context.Leases.FindAsync(model.LeaseId);
            if (lease == null || lease.Status != "Active")
            {
                ModelState.AddModelError("", "Lease not found or is no longer active.");
                ViewBag.ActiveLeases = await _context.Leases
                    .Include(l => l.Tenant).Include(l => l.Unit)
                    .Where(l => l.Status == "Active").ToListAsync();
                return View(model);
            }

            var payment = new Payment
            {
                LeaseId   = model.LeaseId,
                DueDate   = model.DueDate,
                AmountDue = model.AmountDue,
                // amount paid starts at zero because nothing has been received yet
                // the manager records the actual payment later using the Record action
                AmountPaid = 0,
                Status    = "Pending"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Payment record created.";
            return RedirectToAction(nameof(Index));
        }

        // records that money was received for a payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Record(RecordPaymentViewModel model)
        {
            // server side validation runs even though the form has browser validation
            // this protects against anyone bypassing the form and sending a request directly
            if (!ModelState.IsValid)
            {
                TempData["Error"] = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .FirstOrDefault()?.ErrorMessage ?? "Validation failed.";
                return RedirectToAction(nameof(Details), new { id = model.PaymentId });
            }

            var payment = await _context.Payments
                .Include(p => p.Lease)
                .FirstOrDefaultAsync(p => p.PaymentId == model.PaymentId);

            if (payment == null) return NotFound();

            // if the payment is already fully paid we block recording it again
            // this can happen if the manager hits the back button after submitting
            if (payment.Status == "Paid")
            {
                TempData["Error"] = "This payment has already been fully recorded.";
                return RedirectToAction(nameof(Details), new { id = model.PaymentId });
            }

            payment.AmountPaid    = model.AmountPaid;
            payment.PaymentMethod = model.Method;
            payment.ReceiptNumber = model.Receipt;
            payment.PaymentDate   = DateTime.Now;

            // if the tenant paid the full amount the status becomes Paid
            // if they paid less than the full amount the status becomes Partial
            // Partial means the manager can record another instalment later
            payment.Status = model.AmountPaid >= payment.AmountDue ? "Paid" : "Partial";

            await _context.SaveChangesAsync();

            // send the tenant a notification so they know their payment was logged
            await _notificationService.PaymentRecordedAsync(payment.Lease.TenantId, model.AmountPaid);

            TempData["Success"] = $"Payment recorded — status: {payment.Status}.";
            return RedirectToAction(nameof(Details), new { id = model.PaymentId });
        }
    }
}
