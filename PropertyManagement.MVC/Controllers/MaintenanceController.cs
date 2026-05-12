using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Hubs;
using PropertyManagement.API.Models;
using PropertyManagement.API.Services;
using PropertyManagement.MVC.Models;

namespace PropertyManagement.MVC.Controllers
{
    /// <summary>
    /// Handles the live maintenance board (staff/manager), request submission (tenant),
    /// request detail/assign (manager), and status update (staff/manager).
    /// The SignalR hub is hosted in the MVC app so the browser connects same-origin.
    /// </summary>
    public class MaintenanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly NotificationService _notificationService;
        private readonly IHubContext<MaintenanceHub> _hubContext;

        public MaintenanceController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            NotificationService notificationService,
            IHubContext<MaintenanceHub> hubContext)
        {
            _context             = context;
            _userManager         = userManager;
            _notificationService = notificationService;
            _hubContext          = hubContext;
        }

        // ── Live Board ──────────────────────────────────────────────────────

        // renders the live kanban board for managers and maintenance staff
        [Authorize(Roles = "PropertyManager,MaintenanceStaff")]
        public IActionResult Board() => View();

        // returns all non-closed requests as JSON so the board can pre-populate on page load
        // without this the board appears empty until the next real-time push arrives
        [Authorize(Roles = "PropertyManager,MaintenanceStaff")]
        public async Task<IActionResult> OpenRequests()
        {
            var requests = await _context.MaintenanceRequests
                .Include(r => r.Unit)
                .Include(r => r.AssignedStaff)
                .Where(r => r.Status != "Closed")
                .OrderByDescending(r => r.SubmittedDate)
                .Select(r => new {
                    r.RequestId,
                    r.TicketNumber,
                    r.Category,
                    r.Priority,
                    r.Status,
                    AssignedStaff = r.AssignedStaff != null ? r.AssignedStaff.UserName : null
                })
                .ToListAsync();

            return Json(requests);
        }

        // ── Tenant view ─────────────────────────────────────────────────────

        // shows all requests belonging to the logged-in tenant
        // managers and staff get redirected to the board since they see all requests there
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (User.IsInRole("PropertyManager") || User.IsInRole("MaintenanceStaff"))
                return RedirectToAction(nameof(Board));

            // filter by TenantId so tenants can never read each other's requests
            var requests = await _context.MaintenanceRequests
                .Include(r => r.AssignedStaff)
                .Include(r => r.Unit)
                .Where(r => r.TenantId == userId)
                .OrderByDescending(r => r.SubmittedDate)
                .Select(r => new MaintenanceRequestViewModel
                {
                    RequestId         = r.RequestId,
                    TicketNumber      = r.TicketNumber,
                    Category          = r.Category,
                    Priority          = r.Priority,
                    Description       = r.Description,
                    Status            = r.Status,
                    SubmittedDate     = r.SubmittedDate,
                    AssignedStaffName = r.AssignedStaff != null ? r.AssignedStaff.UserName : null,
                    ResolutionNotes   = r.ResolutionNotes,
                    ClosedDate        = r.ClosedDate,
                    UnitNumber        = r.Unit.UnitNumber
                })
                .ToListAsync();

            return View("TenantRequests", requests);
        }

        // ── Submit (Tenant) ─────────────────────────────────────────────────

        // shows the form for tenants to submit a new request
        // only units the tenant holds an active lease for appear in the dropdown
        [Authorize(Roles = "Tenant")]
        public async Task<IActionResult> Create()
        {
            var userId = _userManager.GetUserId(User);

            var tenantUnitIds = await _context.Leases
                .Where(l => l.TenantId == userId && l.Status == "Active")
                .Select(l => l.UnitId)
                .ToListAsync();

            ViewBag.Units = await _context.Units
                .Include(u => u.Building)
                .Where(u => tenantUnitIds.Contains(u.UnitId))
                .Select(u => new { u.UnitId, Display = u.UnitNumber + " — " + u.Building!.Name })
                .ToListAsync();

            ViewBag.Categories = new[] { "Plumbing", "Electrical", "HVAC", "Appliance", "Structural", "General" };
            ViewBag.Priorities  = new[] { "Low", "Medium", "High", "Urgent" };

            return View();
        }

        // saves the submitted request to the database and broadcasts it to the live board
        [HttpPost]
        [Authorize(Roles = "Tenant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubmitMaintenanceViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            // verify the tenant holds an active lease for the selected unit
            // this blocks form manipulation attempts targeting units they don't lease
            var hasLease = await _context.Leases
                .AnyAsync(l => l.TenantId == userId && l.UnitId == model.UnitId && l.Status == "Active");

            if (!hasLease)
                ModelState.AddModelError("UnitId", "You do not have an active lease for the selected unit.");

            if (!ModelState.IsValid)
            {
                var unitIds = await _context.Leases
                    .Where(l => l.TenantId == userId && l.Status == "Active")
                    .Select(l => l.UnitId).ToListAsync();

                ViewBag.Units = await _context.Units
                    .Include(u => u.Building)
                    .Where(u => unitIds.Contains(u.UnitId))
                    .Select(u => new { u.UnitId, Display = u.UnitNumber + " — " + u.Building!.Name })
                    .ToListAsync();

                ViewBag.Categories = new[] { "Plumbing", "Electrical", "HVAC", "Appliance", "Structural", "General" };
                ViewBag.Priorities  = new[] { "Low", "Medium", "High", "Urgent" };
                return View(model);
            }

            // GUID suffix avoids the race condition a sequence-based suffix would have
            var datePrefix   = DateTime.Now.ToString("yyMMdd");
            var uniqueSuffix = Guid.NewGuid().ToString("N")[..6].ToUpper();
            var ticketNumber = $"MNT-{datePrefix}-{uniqueSuffix}";

            var request = new MaintenanceRequest
            {
                TicketNumber  = ticketNumber,
                TenantId      = userId!,
                UnitId        = model.UnitId,
                Category      = model.Category,
                Priority      = model.Priority,
                Description   = model.Description,
                SubmittedDate = DateTime.UtcNow,
                Status        = "Submitted"
            };

            _context.MaintenanceRequests.Add(request);
            await _context.SaveChangesAsync();

            // broadcast to the StaffBoard group so the live board updates instantly
            await _hubContext.Clients.Group("StaffBoard").SendAsync("NewRequestSubmitted", new
            {
                requestId    = request.RequestId,
                ticketNumber = ticketNumber,
                category     = model.Category,
                priority     = model.Priority,
                status       = "Submitted"
            });

            // notify the property manager so they can assign a staff member
            var manager = (await _userManager.GetUsersInRoleAsync("PropertyManager")).FirstOrDefault();
            if (manager != null)
                await _notificationService.MaintenanceSubmittedAsync(manager.Id, ticketNumber);

            TempData["Success"] = $"Request submitted. Your ticket number is {ticketNumber}.";
            return RedirectToAction(nameof(Index));
        }

        // ── Details / Assign / Update Status (Manager + Staff) ──────────────

        /// <summary>GET /Maintenance/Details/5 — full request detail with assign + status panel</summary>
        [Authorize(Roles = "PropertyManager,MaintenanceStaff")]
        public async Task<IActionResult> Details(int id)
        {
            var r = await _context.MaintenanceRequests
                .Include(m => m.Unit).ThenInclude(u => u.Building)
                .Include(m => m.Tenant)
                .Include(m => m.AssignedStaff)
                .FirstOrDefaultAsync(m => m.RequestId == id);

            if (r == null) return NotFound();

            // Load available staff for the assign dropdown (manager only)
            var availableStaff = new List<StaffSelectItem>();
            if (User.IsInRole("PropertyManager"))
            {
                availableStaff = await _context.Users.OfType<MaintenanceStaff>()
                    .OrderBy(s => s.Email)
                    .Select(s => new StaffSelectItem
                    {
                        Id                 = s.Id,
                        Email              = s.Email ?? string.Empty,
                        Skills             = s.Skills ?? string.Empty,
                        AvailabilityStatus = s.AvailabilityStatus ?? "Available"
                    })
                    .ToListAsync();
            }

            var vm = new MaintenanceDetailViewModel
            {
                RequestId         = r.RequestId,
                TicketNumber      = r.TicketNumber,
                Category          = r.Category,
                Priority          = r.Priority,
                Description       = r.Description,
                Status            = r.Status,
                SubmittedDate     = r.SubmittedDate,
                ResolutionNotes   = r.ResolutionNotes,
                ClosedDate        = r.ClosedDate,
                UnitNumber        = r.Unit.UnitNumber,
                BuildingName      = r.Unit.Building.Name,
                TenantEmail       = r.Tenant.Email ?? string.Empty,
                TenantPhone       = r.Tenant.PhoneNumber ?? string.Empty,
                AssignedStaffId   = r.AssignedStaffId,
                AssignedStaffName = r.AssignedStaff?.UserName,
                AvailableStaff    = availableStaff
            };

            return View(vm);
        }

        /// <summary>POST /Maintenance/Assign — property manager assigns staff to a request</summary>
        [HttpPost]
        [Authorize(Roles = "PropertyManager")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int requestId, string staffId)
        {
            var request = await _context.MaintenanceRequests.FindAsync(requestId);
            if (request == null) return NotFound();

            var staff = await _context.Users.OfType<MaintenanceStaff>()
                .FirstOrDefaultAsync(s => s.Id == staffId);
            if (staff == null)
            {
                TempData["Error"] = "Staff member not found.";
                return RedirectToAction(nameof(Details), new { id = requestId });
            }

            // warn but allow if skills don't match the category
            bool skillWarning = false;
            if (!string.IsNullOrWhiteSpace(staff.Skills) && staff.Skills != "[]")
            {
                try
                {
                    var skills = System.Text.Json.JsonSerializer.Deserialize<List<string>>(staff.Skills)
                                 ?? new List<string>();
                    skillWarning = !skills.Contains(request.Category, StringComparer.OrdinalIgnoreCase);
                }
                catch { /* malformed JSON — ignore */ }
            }

            request.AssignedStaffId = staffId;
            request.Status          = "Assigned";
            await _context.SaveChangesAsync();

            // in-system notification to the assigned staff member
            await _notificationService.MaintenanceAssignedAsync(staffId, request.TicketNumber, request.Category);

            // real-time update to all board viewers
            await _hubContext.Clients.Group("StaffBoard").SendAsync("RequestAssigned", new
            {
                requestId     = request.RequestId,
                ticketNumber  = request.TicketNumber,
                assignedStaff = staff.Email,
                newStatus     = "Assigned",
                updatedAt     = DateTime.UtcNow
            });

            TempData["Success"] = skillWarning
                ? $"Assigned to {staff.Email}. Note: their skills may not include '{request.Category}'."
                : $"Request assigned to {staff.Email}.";

            return RedirectToAction(nameof(Details), new { id = requestId });
        }

        /// <summary>POST /Maintenance/UpdateStatus — staff or manager advances the lifecycle</summary>
        [HttpPost]
        [Authorize(Roles = "PropertyManager,MaintenanceStaff")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int requestId, string newStatus, string? resolutionNotes)
        {
            var request = await _context.MaintenanceRequests.FindAsync(requestId);
            if (request == null) return NotFound();

            var validStatuses = new[] { "Submitted", "Assigned", "InProgress", "Resolved", "Closed" };
            if (!validStatuses.Contains(newStatus))
            {
                TempData["Error"] = "Invalid status value.";
                return RedirectToAction(nameof(Details), new { id = requestId });
            }

            // Staff can only update their own assigned requests (unless they are a manager)
            if (User.IsInRole("MaintenanceStaff") && !User.IsInRole("PropertyManager"))
            {
                var staffId = _userManager.GetUserId(User);
                if (request.AssignedStaffId != staffId)
                {
                    TempData["Error"] = "You can only update requests assigned to you.";
                    return RedirectToAction(nameof(Details), new { id = requestId });
                }
            }

            request.Status = newStatus;
            if (!string.IsNullOrWhiteSpace(resolutionNotes))
                request.ResolutionNotes = resolutionNotes;
            if (newStatus == "Closed")
                request.ClosedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // notify tenant when request is resolved
            if (newStatus == "Resolved")
                await _notificationService.MaintenanceResolvedAsync(request.TenantId, request.TicketNumber);

            // real-time board update
            await _hubContext.Clients.Group("StaffBoard").SendAsync("RequestStatusUpdated", new
            {
                requestId    = request.RequestId,
                ticketNumber = request.TicketNumber,
                newStatus    = request.Status,
                updatedAt    = DateTime.UtcNow
            });

            TempData["Success"] = $"Status updated to '{newStatus}'.";
            return RedirectToAction(nameof(Details), new { id = requestId });
        }
    }
}
