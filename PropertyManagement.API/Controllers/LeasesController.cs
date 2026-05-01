using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.DTOs;
using PropertyManagement.API.Models;

namespace PropertyManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeasesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LeasesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Leases
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll([FromQuery] string? status)
        {
            try
            {
                var query = _context.Leases
                    .Include(l => l.Unit)
                        .ThenInclude(u => u.Building)
                    .Include(l => l.Tenant)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(l => l.Status == status);
                }

                var leases = await query
                    .Select(l => new LeaseDto
                    {
                        LeaseId = l.LeaseId,
                        UnitId = l.UnitId,
                        UnitNumber = l.Unit.UnitNumber,
                        BuildingName = l.Unit.Building.Name,
                        TenantId = l.TenantId,
                        TenantEmail = l.Tenant.Email ?? string.Empty,
                        ApplicationDate = l.ApplicationDate,
                        StartDate = l.StartDate,
                        EndDate = l.EndDate,
                        MonthlyRent = l.MonthlyRent,
                        SecurityDeposit = l.SecurityDeposit,
                        Status = l.Status,
                        RejectionReason = l.RejectionReason,
                        ScreeningNotes = l.ScreeningNotes
                    })
                    .OrderByDescending(l => l.ApplicationDate)
                    .ToListAsync();

                return Ok(leases);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving leases", error = ex.Message });
            }
        }

        // GET: api/Leases/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var lease = await _context.Leases
                    .Include(l => l.Unit)
                        .ThenInclude(u => u.Building)
                    .Include(l => l.Tenant)
                    .Where(l => l.LeaseId == id)
                    .Select(l => new LeaseDto
                    {
                        LeaseId = l.LeaseId,
                        UnitId = l.UnitId,
                        UnitNumber = l.Unit.UnitNumber,
                        BuildingName = l.Unit.Building.Name,
                        TenantId = l.TenantId,
                        TenantEmail = l.Tenant.Email ?? string.Empty,
                        ApplicationDate = l.ApplicationDate,
                        StartDate = l.StartDate,
                        EndDate = l.EndDate,
                        MonthlyRent = l.MonthlyRent,
                        SecurityDeposit = l.SecurityDeposit,
                        Status = l.Status,
                        RejectionReason = l.RejectionReason,
                        ScreeningNotes = l.ScreeningNotes
                    })
                    .FirstOrDefaultAsync();

                if (lease == null)
                {
                    return NotFound(new { message = $"Lease with ID {id} not found" });
                }

                return Ok(lease);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving lease", error = ex.Message });
            }
        }

        // POST: api/Leases
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateLeaseDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Verify unit exists and is available
                var unit = await _context.Units.FindAsync(dto.UnitId);
                if (unit == null)
                {
                    return BadRequest(new { message = $"Unit with ID {dto.UnitId} not found" });
                }

                if (unit.AvailabilityStatus != "Available")
                {
                    return BadRequest(new { message = "Unit is not available for lease" });
                }

                // Verify tenant exists
                var tenant = await _context.Users.FindAsync(dto.TenantId);
                if (tenant == null)
                {
                    return BadRequest(new { message = $"Tenant with ID {dto.TenantId} not found" });
                }

                var lease = new Lease
                {
                    UnitId = dto.UnitId,
                    TenantId = dto.TenantId,
                    ApplicationDate = DateTime.Now,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    MonthlyRent = dto.MonthlyRent,
                    SecurityDeposit = dto.SecurityDeposit,
                    Status = "Application", // Initial status
                    ScreeningNotes = dto.ScreeningNotes
                };

                _context.Leases.Add(lease);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = lease.LeaseId }, new { leaseId = lease.LeaseId });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Error creating lease", error = ex.Message });
            }
        }

        // PUT: api/Leases/5
        [HttpPut("{id}")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateLeaseDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var lease = await _context.Leases.FindAsync(id);

                if (lease == null)
                {
                    return NotFound(new { message = $"Lease with ID {id} not found" });
                }

                lease.StartDate = dto.StartDate;
                lease.EndDate = dto.EndDate;
                lease.MonthlyRent = dto.MonthlyRent;
                lease.SecurityDeposit = dto.SecurityDeposit;
                lease.Status = dto.Status;
                lease.ScreeningNotes = dto.ScreeningNotes;
                lease.RejectionReason = dto.RejectionReason;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Error updating lease", error = ex.Message });
            }
        }

        // PUT: api/Leases/5/approve
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var lease = await _context.Leases
                    .Include(l => l.Unit)
                    .FirstOrDefaultAsync(l => l.LeaseId == id);

                if (lease == null)
                {
                    return NotFound(new { message = $"Lease with ID {id} not found" });
                }

                if (lease.Status != "Application")
                {
                    return BadRequest(new { message = "Can only approve leases in Application status" });
                }

                lease.Status = "Active";
                lease.Unit.AvailabilityStatus = "Occupied";
                lease.Unit.CurrentLeaseId = lease.LeaseId;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Lease approved successfully" });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Error approving lease", error = ex.Message });
            }
        }

        // PUT: api/Leases/5/terminate
        [HttpPut("{id}/terminate")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Terminate(int id, [FromBody] TerminateLeaseDto dto)
        {
            try
            {
                var lease = await _context.Leases
                    .Include(l => l.Unit)
                    .FirstOrDefaultAsync(l => l.LeaseId == id);

                if (lease == null)
                {
                    return NotFound(new { message = $"Lease with ID {id} not found" });
                }

                if (lease.Status != "Active")
                {
                    return BadRequest(new { message = "Can only terminate active leases" });
                }

                lease.Status = "Terminated";
                lease.RejectionReason = dto.Reason; // Reuse field for termination reason
                lease.Unit.AvailabilityStatus = "Available";
                lease.Unit.CurrentLeaseId = null;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Lease terminated successfully" });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Error terminating lease", error = ex.Message });
            }
        }

        // DELETE: api/Leases/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var lease = await _context.Leases
                    .Include(l => l.Payments)
                    .FirstOrDefaultAsync(l => l.LeaseId == id);

                if (lease == null)
                {
                    return NotFound(new { message = $"Lease with ID {id} not found" });
                }

                // Check if lease has payments
                if (lease.Payments.Any())
                {
                    return BadRequest(new
                    {
                        message = "Cannot delete lease with payment history",
                        paymentCount = lease.Payments.Count
                    });
                }

                _context.Leases.Remove(lease);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Error deleting lease", error = ex.Message });
            }
        }
    }
}