using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.DTOs;
using PropertyManagement.API.Models;

namespace PropertyManagement.API.Controllers
{
    // manages the top-level building entities in the system
    [Route("api/[controller]")]
    [ApiController]
    public class BuildingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BuildingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET api/Buildings - returns a paginated list of all buildings with their unit counts
        [HttpGet]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 50;
            try
            {
                var total     = await _context.Buildings.CountAsync();
                var buildings = await _context.Buildings
                    .Skip((page - 1) * pageSize).Take(pageSize)
                    .Include(b => b.Units)
                    .Select(b => new BuildingDto
                    {
                        BuildingId     = b.BuildingId,
                        Name           = b.Name,
                        Address        = b.Address,
                        Location       = b.Location,
                        TotalUnits     = b.TotalUnits,
                        AvailableUnits = b.Units.Count(u => u.AvailabilityStatus == "Available"),
                        OccupiedUnits  = b.Units.Count(u => u.AvailabilityStatus == "Occupied")
                    })
                    .ToListAsync();

                return Ok(new { total, page, pageSize, data = buildings });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving buildings", error = ex.Message });
            }
        }

        // GET api/Buildings/5 - returns a single building by its ID
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var building = await _context.Buildings
                    .Include(b => b.Units)
                    .Where(b => b.BuildingId == id)
                    .Select(b => new BuildingDto
                    {
                        BuildingId     = b.BuildingId,
                        Name           = b.Name,
                        Address        = b.Address,
                        Location       = b.Location,
                        TotalUnits     = b.TotalUnits,
                        AvailableUnits = b.Units.Count(u => u.AvailabilityStatus == "Available"),
                        OccupiedUnits  = b.Units.Count(u => u.AvailabilityStatus == "Occupied")
                    })
                    .FirstOrDefaultAsync();

                if (building == null)
                    return NotFound(new { message = $"Building with ID {id} not found" });

                return Ok(building);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving building", error = ex.Message });
            }
        }

        // POST api/Buildings - creates a new building record
        [HttpPost]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Create([FromBody] CreateBuildingDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var building = new Building
                {
                    Name       = dto.Name,
                    Address    = dto.Address,
                    Location   = dto.Location,
                    TotalUnits = dto.TotalUnits
                };

                _context.Buildings.Add(building);
                await _context.SaveChangesAsync();

                var buildingDto = new BuildingDto
                {
                    BuildingId     = building.BuildingId,
                    Name           = building.Name,
                    Address        = building.Address,
                    Location       = building.Location,
                    TotalUnits     = building.TotalUnits,
                    AvailableUnits = 0,
                    OccupiedUnits  = 0
                };

                return CreatedAtAction(nameof(GetById), new { id = building.BuildingId }, buildingDto);
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Error creating building", error = ex.Message });
            }
        }

        // PUT api/Buildings/5 - updates an existing building
        [HttpPut("{id}")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateBuildingDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var building = await _context.Buildings.FindAsync(id);

                if (building == null)
                    return NotFound(new { message = $"Building with ID {id} not found" });

                building.Name       = dto.Name;
                building.Address    = dto.Address;
                building.Location   = dto.Location;
                building.TotalUnits = dto.TotalUnits;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Error updating building", error = ex.Message });
            }
        }

        // DELETE api/Buildings/5 - blocked if the building still has units attached
        // the manager must remove all units from the building first
        [HttpDelete("{id}")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var building = await _context.Buildings
                    .Include(b => b.Units)
                    .FirstOrDefaultAsync(b => b.BuildingId == id);

                if (building == null)
                    return NotFound(new { message = $"Building with ID {id} not found" });

                if (building.Units.Any())
                    return BadRequest(new
                    {
                        message   = "Cannot delete building with existing units",
                        unitCount = building.Units.Count,
                        suggestion = "Delete all units first"
                    });

                _context.Buildings.Remove(building);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Error deleting building", error = ex.Message });
            }
        }
    }
}
