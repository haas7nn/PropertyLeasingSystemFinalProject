using Microsoft.AspNetCore.Mvc;
using PropertyManagement.MVC.Models;
using PropertyManagement.MVC.Services;

namespace PropertyManagement.MVC.Controllers
{
    public class LeasesController : Controller
    {
        private readonly LeaseApiService _leaseService;
        public LeasesController(LeaseApiService leaseService) => _leaseService = leaseService;

        // READ ALL
        public async Task<IActionResult> Index(string? status)
            => View(await _leaseService.GetAllAsync(status));

        // READ ONE
        public async Task<IActionResult> Details(int id)
        {
            var lease = await _leaseService.GetByIdAsync(id);
            return lease == null ? NotFound() : View(lease);
        }

        // CREATE
        public IActionResult Create() => View(new LeaseViewModel());

        [HttpPost]
        public async Task<IActionResult> Create(LeaseViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            if (await _leaseService.CreateAsync(model)) return RedirectToAction(nameof(Index));

            ModelState.AddModelError("", "API Error: Ensure Unit is Available and IDs are correct.");
            return View(model);
        }

        // UPDATE
        public async Task<IActionResult> Edit(int id)
        {
            var lease = await _leaseService.GetByIdAsync(id);
            return lease == null ? NotFound() : View(lease);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(int id, LeaseViewModel model)
        {
         
            model.LeaseId = id;

            if (!ModelState.IsValid) return View(model);

            var success = await _leaseService.UpdateAsync(id, model);
            if (success) return RedirectToAction(nameof(Index));

            ModelState.AddModelError("", "The API refused the update. Check if dates are valid or status transitions are allowed.");
            return View(model);
        }

        // SPECIAL ACTIONS
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var success = await _leaseService.ApproveAsync(id);
            if (!success) TempData["Error"] = "Approval failed. Is the unit available?";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Terminate(int id, string reason)
        {
            var success = await _leaseService.TerminateAsync(id, reason);
            if (!success) TempData["Error"] = "Termination failed.";
            return RedirectToAction(nameof(Index));
        }

    }
}