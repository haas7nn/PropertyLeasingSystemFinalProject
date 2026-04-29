using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models;
using PropertyManagement.MVC.Models;

namespace PropertyManagement.MVC.Controllers
{
    // only the property manager can access anything in this controller
    [Authorize(Roles = "PropertyManager")]
    public class BuildingsController : Controller
    {
        // we use EF Core directly here instead of calling the API
        // buildings are a core part of the system so we manage them locally
        private readonly ApplicationDbContext _context;

        public BuildingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // shows a list of all buildings with their occupancy numbers
        public async Task<IActionResult> Index()
        {
            // we load units alongside each building so we can count them below
            // the counts go into the viewmodel so the view does not need to do any logic
            var buildings = await _context.Buildings
                .Include(b => b.Units)
                .ToListAsync();

            var viewModels = buildings.Select(b => new BuildingViewModel
            {
                BuildingId     = b.BuildingId,
                Name           = b.Name,
                Address        = b.Address,
                Location       = b.Location,
                // count all units in this building
                TotalUnits     = b.Units.Count,
                // count only the ones marked as available
                AvailableUnits = b.Units.Count(u => u.AvailabilityStatus == "Available"),
                // count only the ones currently occupied by a tenant
                OccupiedUnits  = b.Units.Count(u => u.AvailabilityStatus == "Occupied")
            }).ToList();

            return View(viewModels);
        }

        // shows full details for one building
        public async Task<IActionResult> Details(int id)
        {
            // include units so we can show the same occupancy numbers as the index
            var building = await _context.Buildings
                .Include(b => b.Units)
                .FirstOrDefaultAsync(b => b.BuildingId == id);

            if (building == null) return NotFound();

            return View(new BuildingViewModel
            {
                BuildingId     = building.BuildingId,
                Name           = building.Name,
                Address        = building.Address,
                Location       = building.Location,
                TotalUnits     = building.Units.Count,
                AvailableUnits = building.Units.Count(u => u.AvailabilityStatus == "Available"),
                OccupiedUnits  = building.Units.Count(u => u.AvailabilityStatus == "Occupied")
            });
        }

        // shows the empty create form
        public IActionResult Create() => View(new BuildingViewModel());

        // saves the new building to the database
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BuildingViewModel model)
        {
            // if the form has errors we send the user back with the same values filled in
            if (!ModelState.IsValid) return View(model);

            _context.Buildings.Add(new Building
            {
                Name     = model.Name,
                Address  = model.Address,
                Location = model.Location
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Building '{model.Name}' added successfully.";
            return RedirectToAction(nameof(Index));
        }

        // loads the building into the edit form
        public async Task<IActionResult> Edit(int id)
        {
            var b = await _context.Buildings.FindAsync(id);
            if (b == null) return NotFound();

            return View(new BuildingViewModel
            {
                BuildingId = b.BuildingId,
                Name       = b.Name,
                Address    = b.Address,
                Location   = b.Location
            });
        }

        // saves the updated building details
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BuildingViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var building = await _context.Buildings.FindAsync(id);
            if (building == null) return NotFound();

            // update only the fields the form allows
            building.Name     = model.Name;
            building.Address  = model.Address;
            building.Location = model.Location;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Building updated.";
            return RedirectToAction(nameof(Index));
        }

        // shows the delete confirmation page for this building
        public async Task<IActionResult> Delete(int id)
        {
            // include units so the view can show how many units this building has
            // the view also uses this to warn the manager before they try to delete
            var building = await _context.Buildings
                .Include(b => b.Units)
                .FirstOrDefaultAsync(b => b.BuildingId == id);

            if (building == null) return NotFound();

            return View(new BuildingViewModel
            {
                BuildingId     = building.BuildingId,
                Name           = building.Name,
                Address        = building.Address,
                Location       = building.Location,
                TotalUnits     = building.Units.Count,
                AvailableUnits = building.Units.Count(u => u.AvailabilityStatus == "Available"),
                OccupiedUnits  = building.Units.Count(u => u.AvailabilityStatus == "Occupied")
            });
        }

        // actually deletes the building after the manager confirms
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var building = await _context.Buildings
                .Include(b => b.Units)
                .FirstOrDefaultAsync(b => b.BuildingId == id);

            if (building == null) return NotFound();

            // we block deletion if the building still has units attached to it
            // deleting a building with units would leave all those units without a building
            // which would then break any leases, payments, or maintenance records linked to them
            if (building.Units.Any())
            {
                TempData["Error"] = "Cannot delete a building that has units. Remove all units first.";
                return RedirectToAction(nameof(Index));
            }

            _context.Buildings.Remove(building);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Building deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
