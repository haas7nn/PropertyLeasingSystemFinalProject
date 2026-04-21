using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.DTOs;
using PropertyManagement.API.Models;
using System.Security.Claims;

namespace PropertyManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaintenanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MaintenanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // PUBLIC ENDPOINT - No authentication required
        [HttpGet("lookup")]
        [AllowAnonymous]
        public async Task<IActionResult> PublicLookup([FromQuery] string ticketNumber, [FromQuery] string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(ticketNumber) || string.IsNullOrWhiteSpace(phoneNumber))
            {
                return BadRequest(new { message = "Ticket number and phone number are required" });
            }

            var request = await _context.MaintenanceRequests
                .Include(m => m.Unit)
                    .ThenInclude(u => u.Building)
                .Include(m => m.Tenant)
                .Include(m => m.AssignedStaff)
                .FirstOrDefaultAsync(m =>
                    m.TicketNumber == ticketNumber &&
                    m.Tenant.PhoneNumber == phoneNumber);

            if (request == null)
            {
                return NotFound(new { message = "No matching maintenance request found" });
            }

            return Ok(new
            {
                ticketNumber = request.TicketNumber,
                status = request.Status,
                category = request.Category,
                priority = request.Priority,
                description = request.Description,
                submittedDate = request.SubmittedDate,
                building = request.Unit.Building.Name,
                unit = request.Unit.UnitNumber,
                assignedStaff = request.AssignedStaff?.UserName,
                resolutionNotes = request.ResolutionNotes,
                closedDate = request.ClosedDate
            });
        }

        // GET ALL - Requires authentication
        [HttpGet]
        [Authorize(Roles = "PropertyManager,MaintenanceStaff")]
        public async Task<IActionResult> GetAll([FromQuery] string? status = null)
        {
            var query = _context.MaintenanceRequests
                .Include(m => m.Unit)
                    .ThenInclude(u => u.Building)
                .Include(m => m.Tenant)
                .Include(m => m.AssignedStaff)
                .AsQueryable();

            // Filter by status if provided
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(m => m.Status == status);
            }

            var requests = await query
                .OrderByDescending(m => m.SubmittedDate)
                .Select(m => new
                {
                    m.RequestId,
                    m.TicketNumber,
                    m.Category,
                    m.Priority,
                    m.Status,
                    m.Description,
                    m.SubmittedDate,
                    Building = m.Unit.Building.Name,
                    Unit = m.Unit.UnitNumber,
                    Tenant = m.Tenant.Email,
                    AssignedStaff = m.AssignedStaff != null ? m.AssignedStaff.Email : null,
                    m.ResolutionNotes,
                    m.ClosedDate
                })
                .ToListAsync();

            return Ok(requests);
        }

        // GET BY ID
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var request = await _context.MaintenanceRequests
                .Include(m => m.Unit)
                    .ThenInclude(u => u.Building)
                .Include(m => m.Tenant)
                .Include(m => m.AssignedStaff)
                .FirstOrDefaultAsync(m => m.RequestId == id);

            if (request == null)
            {
                return NotFound(new { message = "Maintenance request not found" });
            }

            // Check authorization
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isManager = User.IsInRole("PropertyManager");
            var isStaff = User.IsInRole("MaintenanceStaff");
            var isTenant = User.IsInRole("Tenant");

            // Tenants can only view their own requests
            if (isTenant && request.TenantId != userId)
            {
                return Forbid();
            }

            return Ok(new
            {
                request.RequestId,
                request.TicketNumber,
                request.Category,
                request.Priority,
                request.Status,
                request.Description,
                request.SubmittedDate,
                Building = request.Unit.Building.Name,
                BuildingAddress = request.Unit.Building.Address,
                Unit = request.Unit.UnitNumber,
                Tenant = new
                {
                    request.Tenant.Email,
                    request.Tenant.PhoneNumber
                },
                AssignedStaff = request.AssignedStaff != null ? new
                {
                    request.AssignedStaff.Email,
                    request.AssignedStaff.PhoneNumber,
                    request.AssignedStaff.Skills
                } : null,
                request.ResolutionNotes,
                request.ClosedDate
            });
        }

        // UPDATE STATUS
        [HttpPut("{id}/status")]
        [Authorize(Roles = "PropertyManager,MaintenanceStaff")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateMaintenanceStatusDto dto)
        {
            var request = await _context.MaintenanceRequests.FindAsync(id);

            if (request == null)
            {
                return NotFound(new { message = "Maintenance request not found" });
            }

            // Validate status transition
            var validStatuses = new[] { "Submitted", "Assigned", "InProgress", "Resolved", "Closed" };
            if (!validStatuses.Contains(dto.Status))
            {
                return BadRequest(new { message = "Invalid status" });
            }

            request.Status = dto.Status;

            if (!string.IsNullOrEmpty(dto.ResolutionNotes))
            {
                request.ResolutionNotes = dto.ResolutionNotes;
            }

            if (dto.Status == "Closed")
            {
                request.ClosedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Status updated successfully",
                requestId = request.RequestId,
                newStatus = request.Status
            });
        }

        // ASSIGN STAFF
        [HttpPut("{id}/assign")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> AssignStaff(int id, [FromBody] AssignStaffDto dto)
        {
            var request = await _context.MaintenanceRequests.FindAsync(id);

            if (request == null)
            {
                return NotFound(new { message = "Maintenance request not found" });
            }

            // Verify staff exists and is available
            var staff = await _context.Users.OfType<MaintenanceStaff>()
                .FirstOrDefaultAsync(s => s.Id == dto.StaffId);

            if (staff == null)
            {
                return BadRequest(new { message = "Staff member not found" });
            }

            if (staff.AvailabilityStatus != "Available")
            {
                return BadRequest(new { message = "Staff member is not available" });
            }

            request.AssignedStaffId = dto.StaffId;
            request.Status = "Assigned";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Staff assigned successfully",
                requestId = request.RequestId,
                assignedStaff = staff.Email,
                newStatus = request.Status
            });
        }
    }
}