using Microsoft.AspNetCore.Mvc;
using PropertyManagement.MVC.Models;
using PropertyManagement.MVC.Services;

namespace PropertyManagement.MVC.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly PaymentApiService _service;

        public PaymentsController(PaymentApiService service) => _service = service;

        public async Task<IActionResult> Index(string? status)
        {
            ViewBag.Status = status;
            var payments = status == "Overdue"
                ? await _service.GetOverdueAsync()
                : await _service.GetAllAsync(status);
            return View(payments);
        }

        public async Task<IActionResult> Details(int id)
        {
            var payment = await _service.GetByIdAsync(id);
            return payment == null ? NotFound() : View(payment);
        }

        public IActionResult Create() => View(new PaymentViewModel { DueDate = DateTime.Now });

        [HttpPost]
        public async Task<IActionResult> Create(PaymentViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var success = await _service.CreateAsync(model);
            if (success)
            {
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "Unable to create payment. Please verify the Lease ID exists and is currently active.");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Record(int id, decimal amountPaid, string method, string receipt)
        {
            var request = new RecordPaymentRequest
            {
                AmountPaid = amountPaid,
                PaymentMethod = method,
                ReceiptNumber = receipt
            };
            await _service.RecordPaymentAsync(id, request);
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}