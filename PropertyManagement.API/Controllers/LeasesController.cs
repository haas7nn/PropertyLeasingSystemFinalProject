using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.DTOs;
using PropertyManagement.API.Models;
using PropertyManagement.API.Services;

namespace PropertyManagement.API.Controllers
{
    // API controller for all lease lifecycle operations
    [Route("api/[controller]")]
    [ApiController]
    public class LeasesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;

        public LeasesController(ApplicationDbContext context, NotificationService notificationService)
        {
            _context             = context;
            _notificationService = notificationService;
        }

        // GET api/Leases - returns all leases optionally filtered by status
        // results include unit and building name for display context
        // tenants only see their own leases enforced by the query
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll([FromQuery] string? status)
        {
            var userId    = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isTenant  = User.IsInRole("Tenant");

            var query = _context.Leases
                .Include(l => l.Unit).ThenInclude(u => u.Building)
                .Include(l => l.Tenant)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(l => l.Status == status);

            // tenants can only see leases where they are the tenant
            if (isTenant && userId != null)
                query = query.Where(l => l.TenantId == userId);

            var leases = await query.OrderByDescending(l => l.ApplicationDate)
                .Select(l => new
                {
                    l.LeaseId,      l.UnitId,
                    Unit          = l.Unit.UnitNumber,
                    Building      = l.Unit.Building.Name,
                    l.TenantId,
                    TenantEmail   = l.Tenant.Email,
                    l.ApplicationDate, l.StartDate, l.EndDate,
                    l.MonthlyRent, l.SecurityDeposit,
                    l.Status,      l.RejectionReason, l.ScreeningNotes
                })
                .ToListAsync();

            return Ok(leases);
        }

        // GET api/Leases/5 - returns a single lease with its payments
        // returns 403 Forbidden if a tenant tries to view another tenants lease
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var lease = await _context.Leases
                .Include(l => l.Unit).ThenInclude(u => u.Building)
                .Include(l => l.Tenant)
                .Include(l => l.Payments)
                .FirstOrDefaultAsync(l => l.LeaseId == id);

            if (lease == null)
                return NotFound(new { message = $"Lease with ID {id} not found." });

            // enforce tenant ownership so tenants cannot read each others lease details
            var userId   = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var isTenant = User.IsInRole("Tenant");
            if (isTenant && lease.TenantId != userId)
                return Forbid();

            return Ok(new
            {
                lease.LeaseId,    lease.UnitId,
                Unit            = lease.Unit.UnitNumber,
                Building        = lease.Unit.Building.Name,
                BuildingAddress = lease.Unit.Building.Address,
                lease.TenantId,
                TenantEmail     = lease.Tenant.Email,
                lease.ApplicationDate, lease.StartDate, lease.EndDate,
                lease.MonthlyRent,     lease.SecurityDeposit,
                lease.Status,          lease.RejectionReason, lease.ScreeningNotes,
                Payments = lease.Payments.Select(p => new
                {
                    p.PaymentId, p.DueDate, p.AmountDue, p.AmountPaid, p.Status
                })
            });
        }

        // POST api/Leases - creates a new lease application
        // validates that the target unit is Available before creating
        // also blocks duplicate open applications on the same unit
        [HttpPost]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Create([FromBody] CreateLeaseDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var unit = await _context.Units.FindAsync(dto.UnitId);
            if (unit == null)
                return BadRequest(new { message = $"Unit with ID {dto.UnitId} not found." });
            if (unit.AvailabilityStatus != "Available")
                return BadRequest(new { message = $"Unit {unit.UnitNumber} is not available (current status: {unit.AvailabilityStatus})." });

            // reject if an open application already exists for this unit
            var existingOpen = await _context.Leases.AnyAsync(l =>
                l.UnitId == dto.UnitId &&
                (l.Status == "Application" || l.Status == "Screening"));
            if (existingOpen)
                return BadRequest(new { message = "This unit already has a pending lease application. Reject or approve it first." });

            var lease = new Lease
            {
                UnitId          = dto.UnitId,
                TenantId        = dto.TenantId,
                ApplicationDate = DateTime.Now,
                StartDate       = dto.StartDate,
                EndDate         = dto.EndDate,
                MonthlyRent     = dto.MonthlyRent,
                SecurityDeposit = dto.SecurityDeposit,
                Status          = "Application"
            };

            _context.Leases.Add(lease);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = lease.LeaseId },
                new { leaseId = lease.LeaseId, status = lease.Status });
        }

        // PUT api/Leases/5 - updates mutable fields on an existing lease
        // for status changes use the dedicated lifecycle endpoints below
        [HttpPut("{id}")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateLeaseDto dto)
        {
            var lease = await _context.Leases.FindAsync(id);
            if (lease == null)
                return NotFound(new { message = $"Lease with ID {id} not found." });

            lease.StartDate       = dto.StartDate      ?? lease.StartDate;
            lease.EndDate         = dto.EndDate        ?? lease.EndDate;
            // dto fields are nullable so only update when the caller provided a value
            lease.MonthlyRent     = dto.MonthlyRent.HasValue    && dto.MonthlyRent    > 0 ? dto.MonthlyRent.Value    : lease.MonthlyRent;
            lease.SecurityDeposit = dto.SecurityDeposit.HasValue && dto.SecurityDeposit > 0 ? dto.SecurityDeposit.Value : lease.SecurityDeposit;
            lease.ScreeningNotes  = dto.ScreeningNotes ?? lease.ScreeningNotes;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Lease updated.", leaseId = lease.LeaseId });
        }

        // PUT api/Leases/5/screen - moves a lease from Application to Screening
        // optional screening notes such as CPR check results can be recorded here
        [HttpPut("{id}/screen")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> StartScreening(int id, [FromBody] UpdateLeaseDto dto)
        {
            var lease = await _context.Leases.FindAsync(id);
            if (lease == null)
                return NotFound(new { message = $"Lease with ID {id} not found." });

            if (lease.Status != "Application")
                return BadRequest(new { message = "Only leases in Application status can be moved to Screening." });

            lease.Status        = "Screening";
            lease.ScreeningNotes = dto.ScreeningNotes;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Lease moved to Screening.", leaseId = lease.LeaseId });
        }

        // PUT api/Leases/5/approve - approves a lease from Application or Screening to Active
        // also sets the unit status to Occupied and links CurrentLeaseId
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Approve(int id)
        {
            var lease = await _context.Leases
                .Include(l => l.Unit)
                .FirstOrDefaultAsync(l => l.LeaseId == id);

            if (lease == null)
                return NotFound(new { message = $"Lease with ID {id} not found." });

            if (lease.Status != "Application" && lease.Status != "Screening")
                return BadRequest(new { message = "Only Application or Screening leases can be approved." });

            lease.Status                 = "Active";
            lease.Unit.AvailabilityStatus = "Occupied";
            lease.Unit.CurrentLeaseId    = lease.LeaseId;

            await _context.SaveChangesAsync();

            // notify the tenant that their application has been approved
            await _notificationService.LeaseApprovedAsync(lease.TenantId, lease.Unit.UnitNumber);

            return Ok(new { message = "Lease approved successfully.", leaseId = lease.LeaseId });
        }

        // PUT api/Leases/5/reject - rejects a lease in Application or Screening
        // the unit remains Available because it was never set to Occupied for this lease
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Reject(int id, [FromBody] TerminateLeaseDto dto)
        {
            var lease = await _context.Leases
                .Include(l => l.Unit)
                .FirstOrDefaultAsync(l => l.LeaseId == id);

            if (lease == null)
                return NotFound(new { message = $"Lease with ID {id} not found." });

            if (lease.Status != "Application" && lease.Status != "Screening")
                return BadRequest(new { message = "Only Application or Screening leases can be rejected." });

            lease.Status          = "Rejected";
            lease.RejectionReason = dto.Reason;
            await _context.SaveChangesAsync();

            // notify the tenant with the reason for rejection
            await _notificationService.LeaseRejectedAsync(
                lease.TenantId, lease.Unit.UnitNumber, dto.Reason);

            return Ok(new { message = "Lease rejected.", leaseId = lease.LeaseId });
        }

        // PUT api/Leases/5/terminate - terminates an Active lease
        // also resets the unit back to Available and clears CurrentLeaseId
        [HttpPut("{id}/terminate")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Terminate(int id, [FromBody] TerminateLeaseDto dto)
        {
            var lease = await _context.Leases
                .Include(l => l.Unit)
                .FirstOrDefaultAsync(l => l.LeaseId == id);

            if (lease == null)
                return NotFound(new { message = $"Lease with ID {id} not found." });

            if (lease.Status != "Active")
                return BadRequest(new { message = "Only Active leases can be terminated." });

            lease.Status                  = "Terminated";
            lease.RejectionReason         = dto.Reason;
            lease.Unit.AvailabilityStatus = "Available";
            lease.Unit.CurrentLeaseId     = null;

            await _context.SaveChangesAsync();

            await _notificationService.LeaseTerminatedAsync(lease.TenantId, lease.Unit.UnitNumber);

            return Ok(new { message = "Lease terminated.", leaseId = lease.LeaseId });
        }

        // DELETE api/Leases/5 - permanently removes a lease record
        // active leases cannot be deleted use the Terminate action instead
        [HttpDelete("{id}")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Delete(int id)
        {
            var lease = await _context.Leases.FindAsync(id);
            if (lease == null)
                return NotFound(new { message = $"Lease with ID {id} not found." });

            if (lease.Status == "Active")
                return BadRequest(new { message = "Cannot delete an active lease. Use the Terminate action instead." });

            _context.Leases.Remove(lease);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Lease deleted." });
        }
    }
}
