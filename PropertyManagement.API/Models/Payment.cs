using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models
{
    // represents a single monthly rent installment linked to an active lease

    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        // the lease this payment belongs to
        public int LeaseId { get; set; }

        [ForeignKey("LeaseId")]
        public Lease Lease { get; set; } = null!;

        // the date by which this payment is expected
        // used by OverduePaymentService to automatically flag past-due records
        public DateTime DueDate { get; set; }

        // the rent amount expected for this installment in Bahraini Dinar
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountDue { get; set; }

        // the amount actually received from the tenant in Bahraini Dinar
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        // the date and time the manager physically recorded the payment
        public DateTime? PaymentDate { get; set; }

        // current payment state
        // valid values are Pending Paid Partial and Overdue
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        // how the tenant paid such as Bank Transfer or Cash
        [MaxLength(100)]
        public string? PaymentMethod { get; set; }

        // bank transaction reference or cash receipt number for record keeping
        [MaxLength(100)]
        public string? ReceiptNumber { get; set; }
    }
}
