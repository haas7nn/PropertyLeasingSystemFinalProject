using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.MVC.Models
{
    public class UnitViewModel
    {
        public int UnitId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please select a building.")]
        public int BuildingId { get; set; }

        public string? BuildingName { get; set; }

        [Required]
        public string UnitNumber { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = string.Empty;

        public int? Bedrooms { get; set; }

        public int? Bathrooms { get; set; }

        [Required]
        public decimal SizeInSqFt { get; set; }

        [Required]
        public decimal MonthlyRent { get; set; }

        public string? Amenities { get; set; }

        public string? AvailabilityStatus { get; set; }
    }
}