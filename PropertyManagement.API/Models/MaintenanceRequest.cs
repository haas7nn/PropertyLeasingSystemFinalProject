using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models
{
    public class MaintenanceRequest
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        [MaxLength(20)]
        public string TicketNumber { get; set; } = string.Empty;

        public string TenantId { get; set; } = string.Empty;

        [ForeignKey("TenantId")]
        public Tenant Tenant { get; set; } = null!;

        public int UnitId { get; set; }

        [ForeignKey("UnitId")]
        public Unit Unit { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Priority { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        public DateTime SubmittedDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Submitted";

        public string? AssignedStaffId { get; set; }

        [ForeignKey("AssignedStaffId")]
        public MaintenanceStaff? AssignedStaff { get; set; }

        [MaxLength(2000)]
        public string? ResolutionNotes { get; set; }

        public DateTime? ClosedDate { get; set; }
    }
}