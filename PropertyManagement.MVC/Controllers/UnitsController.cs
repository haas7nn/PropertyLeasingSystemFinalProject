using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models;
using PropertyManagement.MVC.Models;

namespace PropertyManagement.MVC.Controllers
{
    // units can only be managed by the property manager
    [Authorize(Roles = "PropertyManager")]
    public class UnitsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UnitsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // shows all units ordered by building name then unit number so its easy to scan
        public async Task<IActionResult> Index()
        {
            var units = await _context.Units
                .Include(u => u.Building)
                .OrderBy(u => u.Building.Name)
                .ThenBy(u => u.UnitNumber)
                // we project into the viewmodel here so the view only works with viewmodel types
                .Select(u => new UnitViewModel
                {
                    UnitId             = u.UnitId,
                    BuildingId         = u.BuildingId,
                    BuildingName       = u.Building.Name,
                    UnitNumber         = u.UnitNumber,
                    Type               = u.Type,
                    SizeInSqFt         = u.SizeInSqFt,
                    MonthlyRent        = u.MonthlyRent,
                    Amenities          = u.Amenities,
                    AvailabilityStatus = u.AvailabilityStatus,
                    Bedrooms           = u.Bedrooms,
                    Bathrooms          = u.Bathrooms
                })
                .ToListAsync();

            return View(units);
        }

        // shows a single unit with all its details
        public async Task<IActionResult> Details(int id)
        {
            // include the building so we can display its name on the details page
            var unit = await _context.Units
                .Include(u => u.Building)
                .FirstOrDefaultAsync(u => u.UnitId == id);

            if (unit == null) return NotFound();

            return View(new UnitViewModel
            {
                UnitId             = unit.UnitId,
                BuildingId         = unit.BuildingId,
                UnitNumber         = unit.UnitNumber,
                Type               = unit.Type,
                SizeInSqFt         = unit.SizeInSqFt,
                MonthlyRent        = unit.MonthlyRent,
                Amenities          = unit.Amenities,
                AvailabilityStatus = unit.AvailabilityStatus,
                Bedrooms           = unit.Bedrooms
            });
        }

        // loads the create form with the buildings dropdown already filled
        public async Task<IActionResult> Create()
        {
            // the manager needs to pick which building this unit belongs to
            ViewBag.Buildings = await _context.Buildings.ToListAsync();
            return View(new UnitViewModel());
        }

        // saves the new unit to the database
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UnitViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // reload the buildings dropdown before returning the form with errors
                ViewBag.Buildings = await _context.Buildings.ToListAsync();
                return View(model);
            }

            var unit = new Unit
            {
                BuildingId        = model.BuildingId,
                UnitNumber        = model.UnitNumber,
                Type              = model.Type,
                SizeInSqFt        = model.SizeInSqFt,
                MonthlyRent       = model.MonthlyRent,
                Amenities         = model.Amenities,

                // every new unit starts as Available regardless of what the form sends
                // the only way a unit becomes Occupied is when the manager approves a lease
                // we never let the manager set this directly on create to protect that workflow
                AvailabilityStatus = "Available"
            };

            _context.Units.Add(unit);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Unit {model.UnitNumber} created.";
            return RedirectToAction(nameof(Index));
        }

        // loads the edit form pre-filled with the current unit data
        public async Task<IActionResult> Edit(int id)
        {
            var unit = await _context.Units
                .Include(u => u.Building)
                .FirstOrDefaultAsync(u => u.UnitId == id);

            if (unit == null) return NotFound();

            // reload buildings so the dropdown works on the edit form
            ViewBag.Buildings = await _context.Buildings.ToListAsync();

            return View(new UnitViewModel
            {
                UnitId             = unit.UnitId,
                BuildingId         = unit.BuildingId,
                UnitNumber         = unit.UnitNumber,
                Type               = unit.Type,
                SizeInSqFt         = unit.SizeInSqFt,
                MonthlyRent        = unit.MonthlyRent,
                Amenities          = unit.Amenities,
                AvailabilityStatus = unit.AvailabilityStatus
            });
        }

        // saves the updated unit info
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UnitViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Buildings = await _context.Buildings.ToListAsync();
                return View(model);
            }

            var unit = await _context.Units.FindAsync(id);
            if (unit == null) return NotFound();

            // we update all the fields the manager is allowed to change
            unit.BuildingId  = model.BuildingId;
            unit.UnitNumber  = model.UnitNumber;
            unit.Type        = model.Type;
            unit.SizeInSqFt  = model.SizeInSqFt;
            unit.MonthlyRent = model.MonthlyRent;
            unit.Amenities   = model.Amenities;

            // we do not update AvailabilityStatus from here on purpose
            // that field is controlled by the lease workflow only
            // Approve sets it to Occupied and Terminate sets it back to Available

            await _context.SaveChangesAsync();
            TempData["Success"] = "Unit updated.";
            return RedirectToAction(nameof(Index));
        }

        // shows the delete confirmation page for this unit
        public async Task<IActionResult> Delete(int id)
        {
            var unit = await _context.Units
                .Include(u => u.Building)
                .FirstOrDefaultAsync(u => u.UnitId == id);

            if (unit == null) return NotFound();

            return View(new UnitViewModel
            {
                UnitId             = unit.UnitId,
                BuildingId         = unit.BuildingId,
                UnitNumber         = unit.UnitNumber,
                Type               = unit.Type,
                SizeInSqFt         = unit.SizeInSqFt,
                MonthlyRent        = unit.MonthlyRent,
                Amenities          = unit.Amenities,
                AvailabilityStatus = unit.AvailabilityStatus
            });
        }

        // actually removes the unit after the manager confirms
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var unit = await _context.Units
                .Include(u => u.Building)
                .FirstOrDefaultAsync(u => u.UnitId == id);

            if (unit == null) return NotFound();

            // we check if this unit has ever had a lease attached to it
            // even rejected or terminated leases count because we need to keep that history
            // the manager might need it later for billing disputes or compliance checks
            var hasHistory = await _context.Leases.AnyAsync(l => l.UnitId == id);
            if (hasHistory)
            {
                TempData["Error"] = "Cannot delete a unit with lease history. Historical records must be preserved.";
                return RedirectToAction(nameof(Index));
            }

            _context.Units.Remove(unit);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Unit deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
