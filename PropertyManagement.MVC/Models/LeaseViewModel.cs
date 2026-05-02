using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.MVC.Models
{
    public class LeaseViewModel
    {
        public int LeaseId { get; set; }

        [Required]
        [Display(Name = "Unit")]
        public int UnitId { get; set; }
        public string? UnitNumber { get; set; }
        public string? BuildingName { get; set; }

        [Required]
        [Display(Name = "Tenant")]
        public string TenantId { get; set; } = string.Empty;
        public string? TenantEmail { get; set; }

        [Display(Name = "Application Date")]
        public DateTime ApplicationDate { get; set; } = DateTime.Now;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.Today.AddYears(1);

        [Required]
        [Range(0, 1000000)]
        [Display(Name = "Monthly Rent")]
        public decimal MonthlyRent { get; set; }

        [Required]
        [Range(0, 1000000)]
        [Display(Name = "Security Deposit")]
        public decimal SecurityDeposit { get; set; }

        public string Status { get; set; } = "Application";
        public string? RejectionReason { get; set; }
        public string? ScreeningNotes { get; set; }
    }
}