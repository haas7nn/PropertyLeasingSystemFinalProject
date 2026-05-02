using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.MVC.Models
{
    public class PaymentViewModel
    {
        public int PaymentId { get; set; }
        public int LeaseId { get; set; }
        public string TenantEmail { get; set; } = string.Empty;
        public string UnitNumber { get; set; } = string.Empty;

        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; }

        [Display(Name = "Amount Due")]
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
}