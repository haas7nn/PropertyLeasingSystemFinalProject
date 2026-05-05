using Microsoft.AspNetCore.Mvc;
using PropertyManagement.MVC.Services;

namespace PropertyManagement.MVC.Controllers
{
    public class PublicLookupController : Controller
    {
        private readonly MaintenanceApiService _apiService;

        public PublicLookupController(MaintenanceApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(); // This looks for Views/PublicLookup/Index.cshtml
        }

        [HttpPost]
        public async Task<IActionResult> Search(string ticketNumber, string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(ticketNumber) || string.IsNullOrWhiteSpace(phoneNumber))
            {
                ViewBag.Error = "Both ticket number and phone number are required";
                return View("Index");
            }

            var result = await _apiService.LookupMaintenanceRequest(ticketNumber, phoneNumber);

            if (result == null)
            {
                ViewBag.Error = "No matching maintenance request found. Please check your ticket number and phone number.";
                return View("Index");
            }

            return View("Result", result);
        }
    }
}