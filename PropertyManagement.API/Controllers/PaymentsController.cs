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
    public class PaymentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PaymentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Payments
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] int? leaseId)
        {
            try
            {
                var query = _context.Payments
                    .Include(p => p.Lease)
                        .ThenInclude(l => l.Tenant)
                    .Include(p => p.Lease)
                        .ThenInclude(l => l.Unit)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.Status == status);
                }

                if (leaseId.HasValue)
                {
                    query = query.Where(p => p.LeaseId == leaseId.Value);
                }

                var payments = await query
                    .Select(p => new PaymentDto
                    {
                        PaymentId = p.PaymentId,
                        LeaseId = p.LeaseId,
                        TenantEmail = p.Lease.Tenant.Email ?? string.Empty,
                        UnitNumber = p.Lease.Unit.UnitNumber,
                        DueDate = p.DueDate,
                        AmountDue = p.AmountDue,
                        AmountPaid = p.AmountPaid,
                        PaymentDate = p.PaymentDate,
                        Status = p.Status,
                        PaymentMethod = p.PaymentMethod,
                        ReceiptNumber = p.ReceiptNumber
                    })
                    .OrderByDescending(p => p.DueDate)
                    .ToListAsync();

                return Ok(payments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving payments", error = ex.Message });
            }
        }

        // GET: api/Payments/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Lease)
                        .ThenInclude(l => l.Tenant)
                    .Include(p => p.Lease)
                        .ThenInclude(l => l.Unit)
                    .Where(p => p.PaymentId == id)
                    .Select(p => new PaymentDto
                    {
                        PaymentId = p.PaymentId,
                        LeaseId = p.LeaseId,
                        TenantEmail = p.Lease.Tenant.Email ?? string.Empty,
                        UnitNumber = p.Lease.Unit.UnitNumber,
                        DueDate = p.DueDate,
                        AmountDue = p.AmountDue,
                        AmountPaid = p.AmountPaid,
                        PaymentDate = p.PaymentDate,
                        Status = p.Status,
                        PaymentMethod = p.PaymentMethod,
                        ReceiptNumber = p.ReceiptNumber
                    })
                    .FirstOrDefaultAsync();

                if (payment == null)
                {
                    return NotFound(new { message = $"Payment with ID {id} not found" });
                }

                return Ok(payment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving payment", error = ex.Message });
            }
        }

        // GET: api/Payments/overdue
        [HttpGet("overdue")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> GetOverdue()
        {
            try
            {
                var overduePayments = await _context.Payments
                    .Include(p => p.Lease)
                        .ThenInclude(l => l.Tenant)
                    .Include(p => p.Lease)
                        .ThenInclude(l => l.Unit)
                    .Where(p => p.Status == "Overdue" ||
                               (p.Status == "Pending" && p.DueDate < DateTime.Now))
                    .Select(p => new PaymentDto
                    {
                        PaymentId = p.PaymentId,
                        LeaseId = p.LeaseId,
                        TenantEmail = p.Lease.Tenant.Email ?? string.Empty,
                        UnitNumber = p.Lease.Unit.UnitNumber,
                        DueDate = p.DueDate,
                        AmountDue = p.AmountDue,
                        AmountPaid = p.AmountPaid,
                        PaymentDate = p.PaymentDate,
                        Status = p.Status,
                        PaymentMethod = p.PaymentMethod,
                        ReceiptNumber = p.ReceiptNumber
                    })
                    .OrderBy(p => p.DueDate)
                    .ToListAsync();

                return Ok(overduePayments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving overdue payments", error = ex.Message });
            }
        }

        // POST: api/Payments
        [HttpPost]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Create([FromBody] CreatePaymentDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Verify lease exists
                var lease = await _context.Leases.FindAsync(dto.LeaseId);
                if (lease == null)
                {
                    return BadRequest(new { message = $"Lease with ID {dto.LeaseId} not found" });
                }

                var payment = new Payment
                {
                    LeaseId = dto.LeaseId,
                    DueDate = dto.DueDate,
                    AmountDue = dto.AmountDue,
                    AmountPaid = 0,
                    Status = "Pending"
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = payment.PaymentId }, new { paymentId = payment.PaymentId });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Error creating payment", error = ex.Message });
            }
        }

        // POST: api/Payments/5/record
        [HttpPost("{id}/record")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> RecordPayment(int id, [FromBody] RecordPaymentDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var payment = await _context.Payments.FindAsync(id);

                if (payment == null)
                {
                    return NotFound(new { message = $"Payment with ID {id} not found" });
                }

                if (payment.Status == "Paid")
                {
                    return BadRequest(new { message = "Payment has already been recorded" });
                }

                payment.AmountPaid = dto.AmountPaid;
                payment.PaymentDate = DateTime.Now;
                payment.PaymentMethod = dto.PaymentMethod;
                payment.ReceiptNumber = dto.ReceiptNumber;
                payment.Status = dto.AmountPaid >= payment.AmountDue ? "Paid" : "Partial";

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Payment recorded successfully",
                    status = payment.Status,
                    receiptNumber = payment.ReceiptNumber
                });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Error recording payment", error = ex.Message });
            }
        }

        // DELETE: api/Payments/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var payment = await _context.Payments.FindAsync(id);

                if (payment == null)
                {
                    return NotFound(new { message = $"Payment with ID {id} not found" });
                }

                if (payment.Status == "Paid")
                {
                    return BadRequest(new { message = "Cannot delete paid payments" });
                }

                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Error deleting payment", error = ex.Message });
            }
        }
    }
}