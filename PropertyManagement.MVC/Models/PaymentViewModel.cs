using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.MVC.Models
{
    public class PaymentViewModel
    {
        public int PaymentId { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Please select an active lease.")]
        public int LeaseId { get; set; }
        public string TenantEmail { get; set; } = string.Empty;
        public string UnitNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount due must be greater than zero.")]
        [Display(Name = "Amount Due (BD)")]
        public decimal AmountDue { get; set; }

        [Display(Name = "Amount Paid")]
        public decimal AmountPaid { get; set; }

        [Display(Name = "Payment Date")]
        public DateTime? PaymentDate { get; set; }

        public string Status { get; set; } = string.Empty;

        [Display(Name = "Method")]
        public string? PaymentMethod { get; set; }

        [Display(Name = "Receipt #")]
        public string? ReceiptNumber { get; set; }
    }

    public class RecordPaymentRequest
    {
        public decimal AmountPaid { get; set; }
        public string PaymentMethod { get; set; } = "Bank Transfer";
        public string? ReceiptNumber { get; set; }
    }

    // ViewModel for the Record Receipt form on Payments/Details
    // Replaces the raw parameter binding (int id, decimal amountPaid, string method, string? receipt)
    // with proper data annotations so ModelState validation runs server-side
    public class RecordPaymentViewModel
    {
        public int PaymentId { get; set; }

        [Required(ErrorMessage = "Amount paid is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount paid must be greater than zero.")]
        [Display(Name = "Amount Paid (BD)")]
        public decimal AmountPaid { get; set; }

        [Required(ErrorMessage = "Payment method is required.")]
        [Display(Name = "Payment Method")]
        public string Method { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Receipt Number")]
        public string? Receipt { get; set; }
    }
}