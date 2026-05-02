using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.MVC.Models
{
    public class BuildingViewModel
    {
        public int BuildingId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        public string? Location { get; set; }

        public int TotalUnits { get; set; }

        public int AvailableUnits { get; set; }

        public int OccupiedUnits { get; set; }
    }
}