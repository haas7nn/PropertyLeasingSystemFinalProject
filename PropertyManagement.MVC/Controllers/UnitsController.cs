using Microsoft.AspNetCore.Mvc;
using PropertyManagement.MVC.Models;
using PropertyManagement.MVC.Services;

namespace PropertyManagement.MVC.Controllers
{
    public class UnitsController : Controller
    {
        private readonly PropertyApiService _propertyApiService;

        public UnitsController(PropertyApiService propertyApiService)
        {
            _propertyApiService = propertyApiService;
        }

        public async Task<IActionResult> Index()
        {
            var units = await _propertyApiService.GetUnitsAsync();
            return View(units);
        }

        public async Task<IActionResult> Details(int id)
        {
            var unit = await _propertyApiService.GetUnitByIdAsync(id);

            if (unit == null)
                return NotFound();

            return View(unit);
        }

        public async Task<IActionResult> Create()
        {
            var buildings = await _propertyApiService.GetBuildingsAsync();
            ViewBag.Buildings = buildings ?? new List<BuildingViewModel>();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(UnitViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _propertyApiService.CreateUnitAsync(model);

            if (!result)
            {
                ModelState.AddModelError("", "Failed to create unit.");
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var unit = await _propertyApiService.GetUnitByIdAsync(id);

            if (unit == null)
                return NotFound();

            var buildings = await _propertyApiService.GetBuildingsAsync();
            ViewBag.Buildings = buildings;

            return View(unit);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, UnitViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _propertyApiService.UpdateUnitAsync(id, model);

            if (!result)
            {
                ModelState.AddModelError("", "Failed to update unit.");
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var unit = await _propertyApiService.GetUnitByIdAsync(id);

            if (unit == null)
                return NotFound();

            return View(unit);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _propertyApiService.DeleteUnitAsync(id);

            if (!result)
            {
                ModelState.AddModelError("", "Failed to delete unit.");
                return View();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}