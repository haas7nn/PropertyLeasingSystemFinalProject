using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models;

namespace PropertyManagement.MVC.Controllers
{
    public class StaffMvcController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StaffMvcController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var staff = await _context.Users
                .OfType<MaintenanceStaff>()
                .ToListAsync();

            return View(staff);
        }

        public async Task<IActionResult> Edit(string id)
        {
            var staff = await _context.Users
                .OfType<MaintenanceStaff>()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (staff == null)
                return NotFound();

            return View(staff);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, string availabilityStatus)
        {
            var staff = await _context.Users
                .OfType<MaintenanceStaff>()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (staff == null)
                return NotFound();

            staff.AvailabilityStatus = availabilityStatus;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}