using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models
{
    // represents a maintenance request submitted by a tenant for their unit

    public class MaintenanceRequest
    {
        [Key]
        public int RequestId { get; set; }

        // human readable unique ticket reference like MNT-260528-4A9B2C
        [Required]
        [MaxLength(20)]
        public string TicketNumber { get; set; } = string.Empty;

        // the tenant who submitted this request
        public string TenantId { get; set; } = string.Empty;

        [ForeignKey("TenantId")]
        public Tenant Tenant { get; set; } = null!;

        // the unit where the maintenance issue was reported
        public int UnitId { get; set; }

        [ForeignKey("UnitId")]
        public Unit Unit { get; set; } = null!;

        // type of issue such as Plumbing Electrical HVAC or Structural
        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        // urgency level such as Low Medium High or Urgent
        [Required]
        [MaxLength(50)]
        public string Priority { get; set; } = string.Empty;

        // the tenants own description of the problem
        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        // UTC timestamp of when the tenant submitted the request
        public DateTime SubmittedDate { get; set; }

        // current lifecycle stage of this ticket
        // valid values are Submitted Assigned InProgress Resolved and Closed
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Submitted";

        // foreign key to the maintenance staff member assigned to handle this request
        public string? AssignedStaffId { get; set; }

        [ForeignKey("AssignedStaffId")]
        public MaintenanceStaff? AssignedStaff { get; set; }

        // notes written by the assigned staff when they resolve the request
        // visible to the tenant via the public lookup page and the MVC detail view
        [MaxLength(2000)]
        public string? ResolutionNotes { get; set; }

        // timestamp set by the property manager when the ticket is formally closed
        public DateTime? ClosedDate { get; set; }
    }
}
