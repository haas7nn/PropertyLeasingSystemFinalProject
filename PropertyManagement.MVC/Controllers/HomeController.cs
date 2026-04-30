using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.MVC.Models;
using System.Diagnostics;

namespace PropertyManagement.MVC.Controllers
{
    // renders the landing page for guests and the KPI dashboard for logged-in users
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // guests get only the public counters used in the hero section
        // we skip all 8 KPI queries for unauthenticated visitors to avoid
        // wasting DB connections and leaking operational numbers publicly
        public async Task<IActionResult> Index()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return View(new DashboardSummaryViewModel
                {
                    TotalBuildings = await _context.Buildings.CountAsync(),
                    TotalUnits     = await _context.Units.CountAsync(),
                    OccupiedUnits  = await _context.Units.CountAsync(u => u.AvailabilityStatus == "Occupied"),
                    AvailableUnits = await _context.Units.CountAsync(u => u.AvailabilityStatus == "Available"),
                });
            }

            // OverduePayments reads the Status column directly because OverduePaymentService
            // already flags them hourly in the background so we don't recalculate here
            var vm = new DashboardSummaryViewModel
            {
                TotalBuildings          = await _context.Buildings.CountAsync(),
                TotalUnits              = await _context.Units.CountAsync(),
                OccupiedUnits           = await _context.Units.CountAsync(u => u.AvailabilityStatus == "Occupied"),
                AvailableUnits          = await _context.Units.CountAsync(u => u.AvailabilityStatus == "Available"),
                ActiveLeases            = await _context.Leases.CountAsync(l => l.Status == "Active"),
                PendingApplications     = await _context.Leases.CountAsync(l => l.Status == "Application" || l.Status == "Screening"),
                OpenMaintenanceRequests = await _context.MaintenanceRequests.CountAsync(m => m.Status != "Closed"),
                OverduePayments         = await _context.Payments.CountAsync(p => p.Status == "Overdue")
            };
            return View(vm);
        }

        public IActionResult Privacy() => View();

        // standard ASP.NET error handler wired up via UseExceptionHandler in Program.cs
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
