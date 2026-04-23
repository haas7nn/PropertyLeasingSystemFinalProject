using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models
{
    // represents a lease agreement between a tenant and the property company
    // a lease starts as Application and moves through Screening then Active
    // it can also end as Rejected Terminated or remain Active until renewal

    public class Lease
    {
        [Key]
        public int LeaseId { get; set; }

        // the unit being applied for must be Available when the lease is created
        public int UnitId { get; set; }

        [ForeignKey("UnitId")]
        public Unit Unit { get; set; } = null!;

        // the tenant submitting or holding this lease
        public string TenantId { get; set; } = string.Empty;

        [ForeignKey("TenantId")]
        public Tenant Tenant { get; set; } = null!;

        // automatically set to DateTime.Now on creation tenants cannot change this
        public DateTime ApplicationDate { get; set; }

        // intended start of the rental period
        // nullable because it may not be confirmed until after screening and approval
        public DateTime? StartDate { get; set; }

        // intended end of the rental period
        // nullable for the same reason as StartDate
        // expiry checking for renewal reminders compares EndDate to DateTime.Now
        public DateTime? EndDate { get; set; }

        // agreed monthly rent amount in Bahraini Dinar
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyRent { get; set; }

        // security deposit collected upfront typically equal to one or two months rent
        [Column(TypeName = "decimal(18,2)")]
        public decimal SecurityDeposit { get; set; }

        // current lifecycle stage of this lease
        // valid values are Application Screening Active Rejected and Terminated
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Application";

        // reason provided by the property manager when rejecting or terminating
        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        // notes added during the Screening stage such as CPR checks or employer verification
        [MaxLength(1000)]
        public string? ScreeningNotes { get; set; }

        // all rent payment installments linked to this lease
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
