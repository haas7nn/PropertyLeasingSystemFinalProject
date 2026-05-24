using Microsoft.AspNetCore.Mvc;
using PropertyManagement.MVC.Models;
using PropertyManagement.MVC.Services;

namespace PropertyManagement.MVC.Controllers
{
    // allows anyone to track a maintenance request without logging in
    // this is the only MVC controller that calls the API via HttpClient
    // every other controller uses EF Core directly
    public class PublicLookupController : Controller
    {
        private readonly MaintenanceApiService _maintenanceService;

        public PublicLookupController(MaintenanceApiService maintenanceService)
        {
            _maintenanceService = maintenanceService;
        }

        // shows the blank lookup form
        public IActionResult Index() => View();

        // calls the API lookup endpoint and re-renders the Index view with the result
        // returning Index (not Result) keeps the search form visible so the user
        // can search again without hitting the back button
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(string ticketNumber, string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(ticketNumber) || string.IsNullOrWhiteSpace(phoneNumber))
            {
                // pass a sentinel so the view shows the "missing fields" error branch
                return View("Index", new MaintenanceLookupDto { TicketNumber = string.Empty });
            }

            // trim so copy-paste issues with leading or trailing spaces do not cause failed lookups
            var result = await _maintenanceService.LookupMaintenanceRequest(
                ticketNumber.Trim(), phoneNumber.Trim());

            // null means the API returned 404 (no matching record)
            // we return a sentinel with an empty TicketNumber so the view shows the "not found" card
            return View("Index", result ?? new MaintenanceLookupDto { TicketNumber = string.Empty });
        }
    }
}
