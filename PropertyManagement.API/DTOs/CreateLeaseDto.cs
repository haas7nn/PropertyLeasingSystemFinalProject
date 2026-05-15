using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.DTOs
{
    public class CreateLeaseDto
    {
        [Required]
        public int UnitId { get; set; }

        [Required]
        public string TenantId { get; set; } = string.Empty;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [Required]
        [Range(0.01, 1000000)]
        public decimal MonthlyRent { get; set; }

        [Required]
        [Range(0.01, 1000000)]
        public decimal SecurityDeposit { get; set; }

        [StringLength(1000)]
        public string? ScreeningNotes { get; set; }
    }
}