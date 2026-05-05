using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;

namespace PropertyManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PropertyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PropertyController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET ALL BUILDINGS
        [HttpGet("buildings")]
        [Authorize]
        public async Task<IActionResult> GetAllBuildings()
        {
            var buildings = await _context.Buildings
                .Include(b => b.Units)
                .Select(b => new
                {
                    b.BuildingId,
                    b.Name,
                    b.Address,
                    b.Location,
                    b.TotalUnits,
                    AvailableUnits = b.Units.Count(u => u.AvailabilityStatus == "Available"),
                    OccupiedUnits = b.Units.Count(u => u.AvailabilityStatus == "Occupied")
                })
                .ToListAsync();

            return Ok(buildings);
        }

        // GET AVAILABLE UNITS
        [HttpGet("units/available")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailableUnits([FromQuery] string? type = null)
        {
            var query = _context.Units
                .Include(u => u.Building)
                .Where(u => u.AvailabilityStatus == "Available");

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(u => u.Type == type);
            }

            var units = await query
                .Select(u => new
                {
                    u.UnitId,
                    u.UnitNumber,
                    u.Type,
                    u.Bedrooms,
                    u.Bathrooms,
                    u.SizeInSqFt,
                    u.MonthlyRent,
                    u.Amenities,
                    Building = new
                    {
                        u.Building.Name,
                        u.Building.Address,
                        u.Building.Location
                    }
                })
                .ToListAsync();

            return Ok(units);
        }
    }
}