using Microsoft.AspNetCore.Mvc;
using PropertyManagement.Reporting.Models;
using PropertyManagement.Reporting.Services;

namespace PropertyManagement.Reporting.Controllers
{
    /// <summary>
    /// Reporting controller.
    /// All data is fetched exclusively via Web API with JWT authentication (handled by ApiService).
    /// The UI is protected by a login page that validates the manager email + password
    /// against the API's /api/Auth/login endpoint — the JWT returned is stored in the session.
    /// This means the browser login ITSELF is the JWT validation step.
    /// Role: Property Manager only.
    /// </summary>
    public class ReportsController : Controller
    {
        private readonly ApiService _apiService;

        public ReportsController(ApiService apiService)
        {
            _apiService = apiService;
        }

        private bool IsAuthenticated() =>
            HttpContext.Session.GetString("ReportingAuth") == "granted";

        // ── Authentication ──────────────────────────────────────────────────

        // GET /Reports/Login
        public IActionResult Login() => View();

        // POST /Reports/Login
        // Validates by actually calling the API login endpoint (JWT).
        // If the API returns a valid token, the session is granted.
        // This makes the Reporting app's auth directly tied to the API's JWT layer.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Email and password are required.";
                return View();
            }

            // Attempt JWT login against the API — this is the authoritative check
            var tokenResult = await _apiService.TryLoginAsync(email, password);

            if (!tokenResult.Success)
            {
                ViewBag.Error = tokenResult.ErrorMessage ?? "Invalid credentials. Access denied.";
                return View();
            }

            // Verify the returned token belongs to a PropertyManager
            if (!tokenResult.Roles.Contains("PropertyManager"))
            {
                ViewBag.Error = "Access is restricted to the Property Manager role.";
                return View();
            }

            // Store the token in session so ApiService can reuse it for report requests
            HttpContext.Session.SetString("ReportingAuth", "granted");
            HttpContext.Session.SetString("ApiToken", tokenResult.Token ?? string.Empty);

            return RedirectToAction(nameof(Dashboard));
        }

        // POST /Reports/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        // ── Report Pages ────────────────────────────────────────────────────

        // Main dashboard — aggregates all report summaries
        public async Task<IActionResult> Dashboard()
        {
            if (!IsAuthenticated()) return RedirectToAction(nameof(Login));

            var apiConnected = await _apiService.IsApiReachableAsync();
            if (!apiConnected)
                return View(new DashboardViewModel { ApiConnected = false });

            var occupancy        = await _apiService.GetOccupancyReportAsync();
            var maintenanceStats = await _apiService.GetMaintenanceStatsAsync();
            var paymentSummary   = await _apiService.GetPaymentSummaryAsync();

            return View(new DashboardViewModel
            {
                Occupancy        = occupancy,
                MaintenanceStats = maintenanceStats,
                PaymentSummary   = paymentSummary,
                ApiConnected     = true
            });
        }

        // Occupancy report — units/buildings breakdown
        public async Task<IActionResult> Occupancy()
        {
            if (!IsAuthenticated()) return RedirectToAction(nameof(Login));

            var report = await _apiService.GetOccupancyReportAsync();
            if (report == null) { TempData["Error"] = "Unable to load occupancy data from API."; return View(new OccupancyReport()); }
            return View(report);
        }

        // Maintenance statistics — by status / category / resolution time
        public async Task<IActionResult> Maintenance()
        {
            if (!IsAuthenticated()) return RedirectToAction(nameof(Login));

            var report = await _apiService.GetMaintenanceStatsAsync();
            if (report == null) { TempData["Error"] = "Unable to load maintenance data from API."; return View(new MaintenanceStatsReport()); }
            return View(report);
        }

        // Payment summary — collected vs outstanding vs overdue
        public async Task<IActionResult> Payments()
        {
            if (!IsAuthenticated()) return RedirectToAction(nameof(Login));

            var report = await _apiService.GetPaymentSummaryAsync();
            if (report == null) { TempData["Error"] = "Unable to load payment data from API."; return View(new PaymentSummaryReport()); }
            return View(report);
        }
    }
}
