using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.Models
{
    public class Building
    {
        [Key]
        public int BuildingId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Location { get; set; }

        public int TotalUnits { get; set; }

        // Navigation Properties
        public ICollection<Unit> Units { get; set; } = new List<Unit>();
    }
}