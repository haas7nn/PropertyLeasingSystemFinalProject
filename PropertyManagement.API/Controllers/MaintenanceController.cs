using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.DTOs;
using PropertyManagement.API.Hubs;
using PropertyManagement.API.Models;
using PropertyManagement.API.Services;
using System.Security.Claims;

namespace PropertyManagement.API.Controllers
{
    // API Controller: Maintenance Requests
    [Route("api/[controller]")]
    [ApiController]
    public class MaintenanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<MaintenanceHub> _hubContext;
        private readonly NotificationService _notificationService;

        public MaintenanceController(
            ApplicationDbContext context,
            IHubContext<MaintenanceHub> hubContext,
            NotificationService notificationService)
        {
            _context = context;
            _hubContext = hubContext;
            _notificationService = notificationService;
        }

        //  Public Lookup (No Auth)

        [HttpGet("lookup")]
        [AllowAnonymous]
        public async Task<IActionResult> PublicLookup(
            [FromQuery] string ticketNumber,
            [FromQuery] string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(ticketNumber) || string.IsNullOrWhiteSpace(phoneNumber))
                return BadRequest(new { message = "Ticket number and phone number are required." });

            var request = await _context.MaintenanceRequests
                .Include(m => m.Unit).ThenInclude(u => u.Building)
                .Include(m => m.Tenant)
                .Include(m => m.AssignedStaff)
                .FirstOrDefaultAsync(m =>
                    m.TicketNumber == ticketNumber &&
                    m.Tenant.PhoneNumber == phoneNumber);

            if (request == null)
                return NotFound(new { message = "No matching maintenance request found. Please check your ticket number and registered phone number." });

            return Ok(new
            {
                ticketNumber    = request.TicketNumber,
                status          = request.Status,
                category        = request.Category,
                priority        = request.Priority,
                description     = request.Description,
                submittedDate   = request.SubmittedDate,
                building        = request.Unit.Building.Name,
                unit            = request.Unit.UnitNumber,
                assignedStaff   = request.AssignedStaff?.UserName,
                resolutionNotes = request.ResolutionNotes,
                closedDate      = request.ClosedDate
            });
        }

        //  Read Operations (Staff + Manager)

        // returns all requests, optionally filtered by status
        [HttpGet]
        [Authorize(Roles = "PropertyManager,MaintenanceStaff")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll([FromQuery] string? status = null)
        {
            var query = _context.MaintenanceRequests
                .Include(m => m.Unit).ThenInclude(u => u.Building)
                .Include(m => m.Tenant)
                .Include(m => m.AssignedStaff)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(m => m.Status == status);

            var requests = await query
                .OrderByDescending(m => m.SubmittedDate)
                .Select(m => new
                {
                    m.RequestId,    m.TicketNumber,  m.Category,
                    m.Priority,     m.Status,        m.Description,
                    m.SubmittedDate,
                    Building      = m.Unit.Building.Name,
                    Unit          = m.Unit.UnitNumber,
                    Tenant        = m.Tenant.Email,
                    AssignedStaff = m.AssignedStaff != null ? m.AssignedStaff.Email : null,
                    m.ResolutionNotes,
                    m.ClosedDate
                })
                .ToListAsync();

            return Ok(requests);
        }


        // Returns full detail including building address and staff contact info
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var request = await _context.MaintenanceRequests
                .Include(m => m.Unit).ThenInclude(u => u.Building)
                .Include(m => m.Tenant)
                .Include(m => m.AssignedStaff)
                .FirstOrDefaultAsync(m => m.RequestId == id);

            if (request == null)
                return NotFound(new { message = "Maintenance request not found." });

            // Tenants can only see their own requests
            var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isTenant = User.IsInRole("Tenant");
            if (isTenant && request.TenantId != userId)
                return Forbid();

            return Ok(new
            {
                request.RequestId,      request.TicketNumber,
                request.Category,       request.Priority,
                request.Status,         request.Description,
                request.SubmittedDate,
                Building        = request.Unit.Building.Name,
                BuildingAddress = request.Unit.Building.Address,
                Unit            = request.Unit.UnitNumber,
                Tenant          = new { request.Tenant.Email, request.Tenant.PhoneNumber },
                AssignedStaff   = request.AssignedStaff != null ? new
                {
                    request.AssignedStaff.Email,
                    request.AssignedStaff.PhoneNumber,
                    request.AssignedStaff.Skills
                } : null,
                request.ResolutionNotes,
                request.ClosedDate
            });
        }

        // submit

        [HttpPost]
        [Authorize(Roles = "Tenant")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Submit([FromBody] SubmitMaintenanceDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tenantId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (tenantId == null) return Unauthorized();

            var unit = await _context.Units.FindAsync(dto.UnitId);
            if (unit == null)
                return BadRequest(new { message = $"Unit with ID {dto.UnitId} not found." });

            // Generate a collision-safe ticket number using a GUID suffix
            var datePrefix   = DateTime.Now.ToString("yyMMdd");
            var uniqueSuffix = Guid.NewGuid().ToString("N")[..6].ToUpper();
            var ticketNumber = $"MNT-{datePrefix}-{uniqueSuffix}";

            var request = new MaintenanceRequest
            {
                TicketNumber  = ticketNumber,
                TenantId      = tenantId,
                UnitId        = dto.UnitId,
                Category      = dto.Category,
                Priority      = dto.Priority,
                Description   = dto.Description,
                SubmittedDate = DateTime.UtcNow,
                Status        = "Submitted"
            };

            _context.MaintenanceRequests.Add(request);
            await _context.SaveChangesAsync();

            // Real time notify the live staff board of the new request
            await _hubContext.Clients.Group("StaffBoard").SendAsync("NewRequestSubmitted", new
            {
                requestId   = request.RequestId,
                ticketNumber = request.TicketNumber,
                category    = request.Category,
                priority    = request.Priority,
                submittedAt = request.SubmittedDate
            });

            return CreatedAtAction(nameof(GetById), new { id = request.RequestId },
                new { requestId = request.RequestId, ticketNumber = request.TicketNumber });
        }

        //  Update Status (Staff + Manager) 

 
        [HttpPut("{id}/status")]
        [Authorize(Roles = "PropertyManager,MaintenanceStaff")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateMaintenanceStatusDto dto)
        {
            var request = await _context.MaintenanceRequests.FindAsync(id);
            if (request == null)
                return NotFound(new { message = "Maintenance request not found." });

            var validStatuses = new[] { "Submitted", "Assigned", "InProgress", "Resolved", "Closed" };
            if (!validStatuses.Contains(dto.Status))
                return BadRequest(new { message = $"Invalid status. Valid values: {string.Join(", ", validStatuses)}" });

            request.Status = dto.Status;
            if (!string.IsNullOrEmpty(dto.ResolutionNotes))
                request.ResolutionNotes = dto.ResolutionNotes;
            if (dto.Status == "Closed")
                request.ClosedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify tenant their request is resolved and needs confirmation
            if (dto.Status == "Resolved")
                await _notificationService.MaintenanceResolvedAsync(request.TenantId, request.TicketNumber);

            // Real-time: update the live staff board card
            await _hubContext.Clients.Group("StaffBoard").SendAsync("RequestStatusUpdated", new
            {
                requestId    = request.RequestId,
                ticketNumber = request.TicketNumber,
                newStatus    = request.Status,
                updatedAt    = DateTime.UtcNow
            });

            return Ok(new
            {
                message   = "Status updated successfully.",
                requestId = request.RequestId,
                newStatus = request.Status
            });
        }

        // Assign Staff (Manager Only)

  
        [HttpPut("{id}/assign")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> AssignStaff(int id, [FromBody] AssignStaffDto dto)
        {
            var request = await _context.MaintenanceRequests.FindAsync(id);
            if (request == null)
                return NotFound(new { message = "Maintenance request not found." });

            var staff = await _context.Users.OfType<MaintenanceStaff>()
                .FirstOrDefaultAsync(s => s.Id == dto.StaffId);
            if (staff == null)
                return BadRequest(new { message = "Staff member not found." });
            if (staff.AvailabilityStatus != "Available")
                return BadRequest(new { message = "Staff member is not currently available for assignment." });

            // warn if staff skills don't include the request category

            bool skillMatched = true;
            if (!string.IsNullOrWhiteSpace(staff.Skills) && staff.Skills != "[]")
            {
                try
                {
                    var skills = System.Text.Json.JsonSerializer.Deserialize<List<string>>(staff.Skills)
                                 ?? new List<string>();
                    skillMatched = skills.Contains(request.Category, StringComparer.OrdinalIgnoreCase);
                }
                catch { /* Skills JSON malformed — ignore and allow assignment */ }
            }

            request.AssignedStaffId = dto.StaffId;
            request.Status          = "Assigned";
            await _context.SaveChangesAsync();

            // Notify tenant that staff has been assigned
            await _notificationService.MaintenanceAssignedAsync(
                request.TenantId, request.TicketNumber, staff.Email!);

            // Real time: update the live staff board with assignment info
            await _hubContext.Clients.Group("StaffBoard").SendAsync("RequestAssigned", new
            {
                requestId    = request.RequestId,
                ticketNumber = request.TicketNumber,
                assignedStaff = staff.Email,
                newStatus    = "Assigned",
                updatedAt    = DateTime.UtcNow
            });

            return Ok(new
            {
                message       = skillMatched
                                  ? "Staff assigned successfully."
                                  : $"Staff assigned, but note: {staff.Email} has no listed skill for '{request.Category}'. Verify the assignment.",
                skillWarning  = !skillMatched,
                requestId     = request.RequestId,
                assignedStaff = staff.Email,
                newStatus     = request.Status
            });
        }
    }
}
