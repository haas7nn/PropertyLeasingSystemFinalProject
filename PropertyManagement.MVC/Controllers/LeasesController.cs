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
    // all users must be logged in to access anything here
    // individual actions restrict further by role where needed
    [Authorize]
    public class LeasesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly UserManager<IdentityUser> _userManager;

        public LeasesController(
            ApplicationDbContext context,
            NotificationService notificationService,
            UserManager<IdentityUser> userManager)
        {
            _context             = context;
            _notificationService = notificationService;
            _userManager         = userManager;
        }

        // ── Public unit browsing (Tenant) ────────────────────────────────────

        /// <summary>
        /// Tenants browse available units and can click "Apply" on any one.
        /// The brief says: "Prospective tenants browse available units and submit lease applications."
        /// This page fulfils that requirement — it is Tenant-only.
        /// </summary>
        [Authorize(Roles = "Tenant")]
        public async Task<IActionResult> Browse()
        {
            var units = await _context.Units
                .Include(u => u.Building)
                .Where(u => u.AvailabilityStatus == "Available")
                .OrderBy(u => u.Building.Name)
                .ThenBy(u => u.MonthlyRent)
                .Select(u => new UnitViewModel
                {
                    UnitId             = u.UnitId,
                    BuildingId         = u.BuildingId,
                    BuildingName       = u.Building.Name,
                    UnitNumber         = u.UnitNumber,
                    Type               = u.Type,
                    SizeInSqFt         = u.SizeInSqFt,
                    MonthlyRent        = u.MonthlyRent,
                    Amenities          = u.Amenities,
                    Bedrooms           = u.Bedrooms,
                    Bathrooms          = u.Bathrooms,
                    AvailabilityStatus = u.AvailabilityStatus
                })
                .ToListAsync();

            return View(units);
        }

        /// <summary>
        /// GET: Tenant selects a unit and sees a pre-filled application form.
        /// </summary>
        [Authorize(Roles = "Tenant")]
        public async Task<IActionResult> Apply(int unitId)
        {
            var unit = await _context.Units
                .Include(u => u.Building)
                .FirstOrDefaultAsync(u => u.UnitId == unitId);

            if (unit == null || unit.AvailabilityStatus != "Available")
            {
                TempData["Error"] = "This unit is no longer available.";
                return RedirectToAction(nameof(Browse));
            }

            // check whether this tenant already has a pending application on the same unit
            var currentUser = await _userManager.GetUserAsync(User);
            var alreadyApplied = await _context.Leases.AnyAsync(l =>
                l.UnitId == unitId &&
                l.TenantId == currentUser!.Id &&
                (l.Status == "Application" || l.Status == "Screening"));

            if (alreadyApplied)
            {
                TempData["Error"] = "You already have a pending application for this unit.";
                return RedirectToAction(nameof(Index));
            }

            var model = new LeaseViewModel
            {
                UnitId          = unit.UnitId,
                UnitNumber      = unit.UnitNumber,
                BuildingName    = unit.Building.Name,
                TenantId        = currentUser!.Id,
                MonthlyRent     = unit.MonthlyRent,
                SecurityDeposit = unit.MonthlyRent,   // default to 1-month deposit
                StartDate       = DateTime.Today.AddDays(14),
                EndDate         = DateTime.Today.AddDays(14).AddYears(1)
            };

            ViewBag.Unit = unit;
            return View(model);
        }

        /// <summary>
        /// POST: Tenant submits their lease application.
        /// The lease is created with status "Application" — the manager then screens/approves it.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Tenant")]
        public async Task<IActionResult> Apply(LeaseViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // force the TenantId to the logged-in user — never trust the form value
            model.TenantId = currentUser!.Id;

            // remove validation errors for read-only display fields
            ModelState.Remove("UnitNumber");
            ModelState.Remove("BuildingName");
            ModelState.Remove("TenantEmail");

            if (!ModelState.IsValid)
            {
                var unitReload = await _context.Units
                    .Include(u => u.Building)
                    .FirstOrDefaultAsync(u => u.UnitId == model.UnitId);
                ViewBag.Unit = unitReload;
                return View(model);
            }

            var unit = await _context.Units.FindAsync(model.UnitId);
            if (unit == null || unit.AvailabilityStatus != "Available")
            {
                TempData["Error"] = "This unit is no longer available.";
                return RedirectToAction(nameof(Browse));
            }

            // block a second application from the same tenant on the same unit
            var alreadyApplied = await _context.Leases.AnyAsync(l =>
                l.UnitId == model.UnitId &&
                l.TenantId == currentUser.Id &&
                (l.Status == "Application" || l.Status == "Screening"));

            if (alreadyApplied)
            {
                TempData["Error"] = "You already have a pending application for this unit.";
                return RedirectToAction(nameof(Index));
            }

            // also guard against a parallel application from another tenant
            var otherPending = await _context.Leases.AnyAsync(l =>
                l.UnitId == model.UnitId &&
                (l.Status == "Application" || l.Status == "Screening"));

            if (otherPending)
            {
                TempData["Error"] = "Another application is already pending for this unit. Please choose a different unit.";
                return RedirectToAction(nameof(Browse));
            }

            var lease = new Lease
            {
                UnitId          = model.UnitId,
                TenantId        = currentUser.Id,
                ApplicationDate = DateTime.Now,
                StartDate       = model.StartDate,
                EndDate         = model.EndDate,
                MonthlyRent     = model.MonthlyRent,
                SecurityDeposit = model.SecurityDeposit,
                Status          = "Application",
                ScreeningNotes  = model.ScreeningNotes
            };

            _context.Leases.Add(lease);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Your application for Unit {unit.UnitNumber} has been submitted. The property manager will review it shortly.";
            return RedirectToAction(nameof(Index));
        }

        // ── Shared index (all roles) ─────────────────────────────────────────

        public async Task<IActionResult> Index(string? status)
        {
            ViewBag.Status = status;

            var user     = await _userManager.GetUserAsync(User);
            var roles    = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            var isTenant = roles.Contains("Tenant");

            var query = _context.Leases
                .Include(l => l.Unit).ThenInclude(u => u.Building)
                .Include(l => l.Tenant)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(l => l.Status == status);

            // tenants only ever see their own leases
            if (isTenant && user != null)
                query = query.Where(l => l.TenantId == user.Id);

            var leases = await query
                .OrderByDescending(l => l.ApplicationDate)
                .Select(l => new LeaseViewModel
                {
                    LeaseId         = l.LeaseId,
                    UnitId          = l.UnitId,
                    UnitNumber      = l.Unit.UnitNumber,
                    BuildingName    = l.Unit.Building.Name,
                    TenantId        = l.TenantId,
                    TenantEmail     = l.Tenant.Email,
                    ApplicationDate = l.ApplicationDate,
                    StartDate       = l.StartDate ?? DateTime.Today,
                    EndDate         = l.EndDate   ?? DateTime.Today.AddYears(1),
                    MonthlyRent     = l.MonthlyRent,
                    SecurityDeposit = l.SecurityDeposit,
                    Status          = l.Status,
                    RejectionReason = l.RejectionReason,
                    ScreeningNotes  = l.ScreeningNotes
                })
                .ToListAsync();

            return View(leases);
        }

        public async Task<IActionResult> Details(int id)
        {
            var lease = await _context.Leases
                .Include(l => l.Unit).ThenInclude(u => u.Building)
                .Include(l => l.Tenant)
                .FirstOrDefaultAsync(l => l.LeaseId == id);

            if (lease == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (User.IsInRole("Tenant") && lease.TenantId != currentUser?.Id)
                return Forbid();

            return View(MapToViewModel(lease));
        }

        // ── Manager-only CRUD ────────────────────────────────────────────────

        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Create()
        {
            ViewBag.AvailableUnits = await _context.Units
                .Include(u => u.Building)
                .Where(u => u.AvailabilityStatus == "Available")
                .ToListAsync();

            ViewBag.Tenants = await _context.Users.OfType<Tenant>().ToListAsync();
            return View(new LeaseViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Create(LeaseViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.AvailableUnits = await _context.Units.Include(u => u.Building)
                    .Where(u => u.AvailabilityStatus == "Available").ToListAsync();
                ViewBag.Tenants = await _context.Users.OfType<Tenant>().ToListAsync();
                return View(model);
            }

            var unit = await _context.Units.FindAsync(model.UnitId);
            if (unit == null || unit.AvailabilityStatus != "Available")
            {
                ModelState.AddModelError("", "Unit is no longer available.");
                ViewBag.AvailableUnits = await _context.Units.Include(u => u.Building)
                    .Where(u => u.AvailabilityStatus == "Available").ToListAsync();
                ViewBag.Tenants = await _context.Users.OfType<Tenant>().ToListAsync();
                return View(model);
            }

            var existingOpen = await _context.Leases.AnyAsync(l =>
                l.UnitId == model.UnitId &&
                (l.Status == "Application" || l.Status == "Screening"));

            if (existingOpen)
            {
                ModelState.AddModelError("", "This unit already has a pending application. Resolve it before creating another.");
                ViewBag.AvailableUnits = await _context.Units.Include(u => u.Building)
                    .Where(u => u.AvailabilityStatus == "Available").ToListAsync();
                ViewBag.Tenants = await _context.Users.OfType<Tenant>().ToListAsync();
                return View(model);
            }

            var lease = new Lease
            {
                UnitId          = model.UnitId,
                TenantId        = model.TenantId,
                ApplicationDate = DateTime.Now,
                StartDate       = model.StartDate,
                EndDate         = model.EndDate,
                MonthlyRent     = model.MonthlyRent,
                SecurityDeposit = model.SecurityDeposit,
                Status          = "Application",
                ScreeningNotes  = model.ScreeningNotes
            };

            _context.Leases.Add(lease);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Lease application created.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Edit(int id)
        {
            var lease = await _context.Leases
                .Include(l => l.Unit).ThenInclude(u => u.Building)
                .Include(l => l.Tenant)
                .FirstOrDefaultAsync(l => l.LeaseId == id);

            if (lease == null) return NotFound();
            return View(MapToViewModel(lease));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Edit(int id, LeaseViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var lease = await _context.Leases.FindAsync(id);
            if (lease == null) return NotFound();

            lease.StartDate       = model.StartDate;
            lease.EndDate         = model.EndDate;
            lease.MonthlyRent     = model.MonthlyRent;
            lease.SecurityDeposit = model.SecurityDeposit;
            lease.ScreeningNotes  = model.ScreeningNotes;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Lease updated.";
            return RedirectToAction(nameof(Index));
        }

        // ── Lifecycle actions (manager only) ─────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Screen(int id, string? screeningNotes)
        {
            var lease = await _context.Leases.FindAsync(id);
            if (lease == null) return NotFound();

            if (lease.Status != "Application")
            {
                TempData["Error"] = "Only Application leases can be moved to Screening.";
                return RedirectToAction(nameof(Index));
            }

            lease.Status         = "Screening";
            lease.ScreeningNotes = screeningNotes;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Lease moved to Screening.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Approve(int id)
        {
            var lease = await _context.Leases
                .Include(l => l.Unit)
                .FirstOrDefaultAsync(l => l.LeaseId == id);

            if (lease == null) return NotFound();

            if (lease.Status != "Application" && lease.Status != "Screening")
            {
                TempData["Error"] = "Only Application or Screening leases can be approved.";
                return RedirectToAction(nameof(Index));
            }

            lease.Status                  = "Active";
            lease.Unit.AvailabilityStatus = "Occupied";
            lease.Unit.CurrentLeaseId     = lease.LeaseId;

            await _context.SaveChangesAsync();
            await _notificationService.LeaseApprovedAsync(lease.TenantId, lease.Unit.UnitNumber);

            TempData["Success"] = "Lease approved successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var lease = await _context.Leases
                .Include(l => l.Unit)
                .FirstOrDefaultAsync(l => l.LeaseId == id);

            if (lease == null) return NotFound();

            if (lease.Status != "Application" && lease.Status != "Screening")
            {
                TempData["Error"] = "Only Application or Screening leases can be rejected.";
                return RedirectToAction(nameof(Index));
            }

            lease.Status          = "Rejected";
            lease.RejectionReason = reason;

            await _context.SaveChangesAsync();
            await _notificationService.LeaseRejectedAsync(
                lease.TenantId, lease.Unit.UnitNumber, reason);

            TempData["Success"] = "Lease rejected.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Renew(int id, DateTime newEndDate)
        {
            var lease = await _context.Leases
                .Include(l => l.Unit)
                .FirstOrDefaultAsync(l => l.LeaseId == id);

            if (lease == null) return NotFound();

            if (lease.Status != "Active")
            {
                TempData["Error"] = "Only active leases can be renewed.";
                return RedirectToAction(nameof(Index));
            }

            if (newEndDate <= (lease.EndDate ?? DateTime.Today))
            {
                TempData["Error"] = "New end date must be after the current end date.";
                return RedirectToAction(nameof(Details), new { id });
            }

            lease.EndDate = newEndDate;
            await _context.SaveChangesAsync();
            await _notificationService.LeaseRenewedAsync(
                lease.TenantId, lease.Unit.UnitNumber, newEndDate);

            TempData["Success"] = $"Lease renewed to {newEndDate:dd MMM yyyy}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Terminate(int id, string reason)
        {
            var lease = await _context.Leases
                .Include(l => l.Unit)
                .FirstOrDefaultAsync(l => l.LeaseId == id);

            if (lease == null) return NotFound();

            if (lease.Status != "Active")
            {
                TempData["Error"] = "Only active leases can be terminated.";
                return RedirectToAction(nameof(Index));
            }

            lease.Status                  = "Terminated";
            lease.RejectionReason         = reason;
            lease.Unit.AvailabilityStatus = "Available";
            lease.Unit.CurrentLeaseId     = null;

            await _context.SaveChangesAsync();
            await _notificationService.LeaseTerminatedAsync(
                lease.TenantId, lease.Unit.UnitNumber);

            TempData["Success"] = "Lease terminated. Unit is now available.";
            return RedirectToAction(nameof(Index));
        }

        // ── Helper ───────────────────────────────────────────────────────────

        private static LeaseViewModel MapToViewModel(Lease l) => new()
        {
            LeaseId         = l.LeaseId,
            UnitId          = l.UnitId,
            UnitNumber      = l.Unit?.UnitNumber,
            BuildingName    = l.Unit?.Building?.Name,
            TenantId        = l.TenantId,
            TenantEmail     = l.Tenant?.Email,
            ApplicationDate = l.ApplicationDate,
            StartDate       = l.StartDate ?? DateTime.Today,
            EndDate         = l.EndDate   ?? DateTime.Today.AddYears(1),
            MonthlyRent     = l.MonthlyRent,
            SecurityDeposit = l.SecurityDeposit,
            Status          = l.Status,
            RejectionReason = l.RejectionReason,
            ScreeningNotes  = l.ScreeningNotes
        };
    }
}
