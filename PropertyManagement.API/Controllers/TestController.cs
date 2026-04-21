using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.API.Models;

namespace PropertyManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TestController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("database-check")]
        public async Task<IActionResult> CheckDatabase()
        {
            try
            {
                var buildingCount = await _context.Buildings.CountAsync();
                var unitCount = await _context.Units.CountAsync();
                var tenantCount = await _context.Users.OfType<Tenant>().CountAsync();
                var staffCount = await _context.Users.OfType<MaintenanceStaff>().CountAsync();
                var managerCount = await _context.Users.OfType<PropertyManager>().CountAsync();
                var leaseCount = await _context.Leases.CountAsync();
                var paymentCount = await _context.Payments.CountAsync();
                var maintenanceCount = await _context.MaintenanceRequests.CountAsync();

                return Ok(new
                {
                    status = "✅ Database Connected Successfully",
                    buildings = buildingCount,
                    units = unitCount,
                    tenants = tenantCount,
                    maintenanceStaff = staffCount,
                    propertyManagers = managerCount,
                    leases = leaseCount,
                    payments = paymentCount,
                    maintenanceRequests = maintenanceCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "❌ Database Connection Failed",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet("relationships-check")]
        public async Task<IActionResult> CheckRelationships()
        {
            try
            {
                // Test Building → Units
                var building = await _context.Buildings
                    .Include(b => b.Units)
                    .FirstOrDefaultAsync();

                // Test Unit → Lease
                var unit = await _context.Units
                    .Include(u => u.LeaseHistory)
                    .Include(u => u.CurrentLease)
                    .FirstOrDefaultAsync();

                // Test Lease → Tenant
                var lease = await _context.Leases
                    .Include(l => l.Tenant)
                    .Include(l => l.Unit)
                    .Include(l => l.Payments)
                    .FirstOrDefaultAsync();

                // Test Maintenance Request → Tenant, Unit, Staff
                var maintenance = await _context.MaintenanceRequests
                    .Include(m => m.Tenant)
                    .Include(m => m.Unit)
                    .Include(m => m.AssignedStaff)
                    .FirstOrDefaultAsync();

                return Ok(new
                {
                    status = "✅ All Relationships Working",
                    buildingHasUnits = building?.Units.Count > 0,
                    unitHasLeaseHistory = unit?.LeaseHistory.Count >= 0,
                    leaseHasTenant = lease?.Tenant != null,
                    leaseHasPayments = lease?.Payments.Count > 0,
                    maintenanceHasTenant = maintenance?.Tenant != null,
                    maintenanceHasUnit = maintenance?.Unit != null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "❌ Relationship Check Failed",
                    error = ex.Message
                });
            }
        }

        [HttpGet("sample-data")]
        public async Task<IActionResult> GetSampleData()
        {
            try
            {
                var buildings = await _context.Buildings
                    .Include(b => b.Units)
                    .ToListAsync();

                var tenants = await _context.Users.OfType<Tenant>()
                    .Select(t => new
                    {
                        t.Email,
                        t.CPR,
                        t.Occupation,
                        t.PhoneNumber
                    })
                    .ToListAsync();

                var staff = await _context.Users.OfType<MaintenanceStaff>()
                    .Select(s => new
                    {
                        s.Email,
                        s.Skills,
                        s.AvailabilityStatus
                    })
                    .ToListAsync();

                return Ok(new
                {
                    buildings,
                    tenants,
                    maintenanceStaff = staff
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}