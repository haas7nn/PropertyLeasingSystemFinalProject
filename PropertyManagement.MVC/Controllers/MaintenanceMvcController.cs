using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models;
using System.Security.Claims;
using System.Text.Json;
using System.Text;


namespace PropertyManagement.MVC.Controllers
{
    
    public class MaintenanceMvcController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MaintenanceMvcController(ApplicationDbContext context)
        {
            _context = context;
        }

        // View own maintenance requests
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var requests = await _context.MaintenanceRequests
                .Include(m => m.Unit)
                .Where(m => m.TenantId == userId)
                .OrderByDescending(m => m.SubmittedDate)
                .ToListAsync();

            return View(requests);
        }

        // Show create form
        public IActionResult Create()
        {
            return View();
        }

        // Submit maintenance request
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MaintenanceRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            request.TenantId = userId;
            request.TicketNumber = "MNT-" + DateTime.Now.Ticks.ToString().Substring(8, 6);
            request.Status = "Submitted";
            request.SubmittedDate = DateTime.Now;

            _context.MaintenanceRequests.Add(request);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> All(string? status)
        {
            var query = _context.MaintenanceRequests
                .Include(m => m.Unit)
                .Include(m => m.Tenant)
                .Include(m => m.AssignedStaff)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(m => m.Status == status);
            }

            var requests = await query
                .OrderByDescending(m => m.SubmittedDate)
                .ToListAsync();

            return View(requests);
        }
        public async Task<IActionResult> Assign(int id)
        {
            var request = await _context.MaintenanceRequests
                .Include(m => m.Unit)
                .FirstOrDefaultAsync(m => m.RequestId == id);

            if (request == null)
                return NotFound();

            var availableStaff = await _context.Users
                .OfType<MaintenanceStaff>()
                .Where(s => s.AvailabilityStatus == "Available" &&
                            s.Skills.Contains(request.Category))
                .ToListAsync();

            ViewBag.StaffList = availableStaff;
            return View(request);
        }
        [HttpPost]
        public async Task<IActionResult> AssignConfirmed(int requestId, string staffId)
        {
            var request = await _context.MaintenanceRequests.FindAsync(requestId);

            if (request == null)
                return NotFound();

            request.AssignedStaffId = staffId;
            request.Status = "Assigned";

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(All));
        }
        public async Task<IActionResult> MyAssignments()
        {
            var staffId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var requests = await _context.MaintenanceRequests
                .Include(m => m.Unit)
                .Include(m => m.Tenant)
                .Where(m => m.AssignedStaffId == staffId)
                .OrderByDescending(m => m.SubmittedDate)
                .ToListAsync();

            return View(requests);
        }
        public async Task<IActionResult> UpdateStatus(int id)
        {
            var request = await _context.MaintenanceRequests
                .Include(m => m.Unit)
                .Include(m => m.Tenant)
                .FirstOrDefaultAsync(m => m.RequestId == id);

            if (request == null)
                return NotFound();

            return View(request);
        }
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status, string? resolutionNotes)
        {
            var client = new HttpClient();
            var apiBaseUrl = "https://localhost:7168";

            var updateDto = new
            {
                Status = status,
                ResolutionNotes = resolutionNotes
            };

            var content = new StringContent(
                JsonSerializer.Serialize(updateDto),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.PutAsync(
                $"{apiBaseUrl}/api/Maintenance/{id}/status",
                content);

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Failed to update status");
                return View();
            }

            return RedirectToAction(nameof(MyAssignments));
        }
    }
}