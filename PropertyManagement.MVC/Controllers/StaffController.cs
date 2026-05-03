using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models;
using PropertyManagement.MVC.Models;

namespace PropertyManagement.MVC.Controllers
{
    /// <summary>
    /// Property Manager only — manages MaintenanceStaff accounts and profiles.
    /// The brief states the senior business role (PropertyManager) owns user management.
    /// </summary>
    [Authorize(Roles = "PropertyManager")]
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public StaffController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context     = context;
            _userManager = userManager;
        }

        // GET /Staff — list all maintenance staff with active request count
        public async Task<IActionResult> Index()
        {
            var staffList = await _context.Users
                .OfType<MaintenanceStaff>()
                .Select(s => new StaffViewModel
                {
                    Id                 = s.Id,
                    Email              = s.Email ?? string.Empty,
                    PhoneNumber        = s.PhoneNumber,
                    Skills             = s.Skills ?? string.Empty,
                    AvailabilityStatus = s.AvailabilityStatus ?? "Available",
                    ActiveRequests     = _context.MaintenanceRequests
                                            .Count(r => r.AssignedStaffId == s.Id &&
                                                        r.Status != "Closed" && r.Status != "Resolved")
                })
                .ToListAsync();

            return View(staffList);
        }

        // GET /Staff/Create — show create form
        public IActionResult Create() => View(new CreateStaffViewModel());

        // POST /Staff/Create — create a new MaintenanceStaff account
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateStaffViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Normalise skills: turn "Plumbing, Electrical" → JSON array ["Plumbing","Electrical"]
            var skillList = model.Skills
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
            var skillsJson = System.Text.Json.JsonSerializer.Serialize(skillList);

            var staff = new MaintenanceStaff
            {
                UserName           = model.Email,
                Email              = model.Email,
                PhoneNumber        = model.PhoneNumber,
                Skills             = skillsJson,
                AvailabilityStatus = "Available",
                EmailConfirmed     = true
            };

            var result = await _userManager.CreateAsync(staff, model.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(staff, "MaintenanceStaff");
            TempData["Success"] = $"Staff account for {model.Email} created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Staff/Edit/5 — edit staff profile
        public async Task<IActionResult> Edit(string id)
        {
            var staff = await _context.Users.OfType<MaintenanceStaff>()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (staff == null) return NotFound();

            // Parse stored JSON skills back to a comma-separated display string
            var displaySkills = string.Empty;
            if (!string.IsNullOrWhiteSpace(staff.Skills))
            {
                try
                {
                    var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(staff.Skills);
                    displaySkills = parsed != null ? string.Join(", ", parsed) : staff.Skills;
                }
                catch { displaySkills = staff.Skills; }
            }

            return View(new EditStaffViewModel
            {
                Id                 = staff.Id,
                Email              = staff.Email ?? string.Empty,
                PhoneNumber        = staff.PhoneNumber,
                Skills             = displaySkills,
                AvailabilityStatus = staff.AvailabilityStatus ?? "Available"
            });
        }

        // POST /Staff/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditStaffViewModel model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var staff = await _context.Users.OfType<MaintenanceStaff>()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (staff == null) return NotFound();

            // Normalise skills to JSON array
            var skillList = model.Skills
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            staff.PhoneNumber        = model.PhoneNumber;
            staff.Skills             = System.Text.Json.JsonSerializer.Serialize(skillList);
            staff.AvailabilityStatus = model.AvailabilityStatus;

            // If email changed, update username and normalised email
            if (staff.Email != model.Email)
            {
                staff.Email              = model.Email;
                staff.UserName           = model.Email;
                staff.NormalizedEmail    = model.Email.ToUpperInvariant();
                staff.NormalizedUserName = model.Email.ToUpperInvariant();
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Staff profile updated.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Staff/Details/5 — view staff profile and assigned requests
        public async Task<IActionResult> Details(string id)
        {
            var staff = await _context.Users.OfType<MaintenanceStaff>()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (staff == null) return NotFound();

            ViewBag.AssignedRequests = await _context.MaintenanceRequests
                .Include(r => r.Unit).ThenInclude(u => u.Building)
                .Where(r => r.AssignedStaffId == id)
                .OrderByDescending(r => r.SubmittedDate)
                .Select(r => new MaintenanceRequestViewModel
                {
                    RequestId     = r.RequestId,
                    TicketNumber  = r.TicketNumber,
                    Category      = r.Category,
                    Priority      = r.Priority,
                    Status        = r.Status,
                    SubmittedDate = r.SubmittedDate,
                    UnitNumber    = r.Unit.UnitNumber
                })
                .ToListAsync();

            // Parse skills for display
            var displaySkills = string.Empty;
            if (!string.IsNullOrWhiteSpace(staff.Skills))
            {
                try
                {
                    var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(staff.Skills);
                    displaySkills = parsed != null ? string.Join(", ", parsed) : staff.Skills;
                }
                catch { displaySkills = staff.Skills; }
            }

            return View(new EditStaffViewModel
            {
                Id                 = staff.Id,
                Email              = staff.Email ?? string.Empty,
                PhoneNumber        = staff.PhoneNumber,
                Skills             = displaySkills,
                AvailabilityStatus = staff.AvailabilityStatus ?? "Available"
            });
        }

        // POST /Staff/Delete/5 — delete staff account (only if no open requests)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var hasOpen = await _context.MaintenanceRequests
                .AnyAsync(r => r.AssignedStaffId == id &&
                               r.Status != "Closed" && r.Status != "Resolved");

            if (hasOpen)
            {
                TempData["Error"] = "Cannot delete a staff member with open requests. Reassign or close them first.";
                return RedirectToAction(nameof(Index));
            }

            var staff = await _userManager.FindByIdAsync(id);
            if (staff != null)
                await _userManager.DeleteAsync(staff);

            TempData["Success"] = "Staff account removed.";
            return RedirectToAction(nameof(Index));
        }
    }
}
