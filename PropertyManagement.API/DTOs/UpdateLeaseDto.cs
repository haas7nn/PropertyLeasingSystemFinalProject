using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.DTOs
{
    public class UpdateLeaseDto
    {
        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Required]
        [Range(0.01, 1000000)]
        public decimal MonthlyRent { get; set; }

        [Required]
        [Range(0.01, 1000000)]
        public decimal SecurityDeposit { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty;

        [StringLength(500)]
        public string? RejectionReason { get; set; }

        [StringLength(1000)]
        public string? ScreeningNotes { get; set; }
    }
}