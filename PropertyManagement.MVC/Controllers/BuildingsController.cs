using Microsoft.AspNetCore.Mvc;
using PropertyManagement.MVC.Models;
using PropertyManagement.MVC.Services;

namespace PropertyManagement.MVC.Controllers
{
    public class BuildingsController : Controller
    {
        private readonly PropertyApiService _propertyApiService;

        public BuildingsController(PropertyApiService propertyApiService)
        {
            _propertyApiService = propertyApiService;
        }

        public async Task<IActionResult> Index()
        {
            var buildings = await _propertyApiService.GetBuildingsAsync();
            return View(buildings);
        }

        public async Task<IActionResult> Details(int id)
        {
            var building = await _propertyApiService.GetBuildingByIdAsync(id);

            if (building == null)
                return NotFound();

            return View(building);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BuildingViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _propertyApiService.CreateBuildingAsync(model);

            if (!result)
            {
                ModelState.AddModelError("", "Failed to create building.");
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var building = await _propertyApiService.GetBuildingByIdAsync(id);

            if (building == null)
                return NotFound();

            return View(building);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BuildingViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _propertyApiService.UpdateBuildingAsync(id, model);

            if (!result)
            {
                ModelState.AddModelError("", "Failed to update building.");
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var building = await _propertyApiService.GetBuildingByIdAsync(id);

            if (building == null)
                return NotFound();

            return View(building);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _propertyApiService.DeleteBuildingAsync(id);

            if (!result)
            {
                ModelState.AddModelError("", "Failed to delete building.");
                return View();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}