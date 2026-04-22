using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.Models
{
    // represents a physical building managed by the property company

    public class Building
    {
        [Key]
        public int BuildingId { get; set; }

        // display name of the building shown everywhere in the UI
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        // full civic address entered manually by the property manager
        // not validated against any map API just a text field for display
        [Required]
        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        // optional area or district label used by the reporting app to group buildings
        [MaxLength(100)]
        public string? Location { get; set; }

        // informational counter of how many units exist in this building
        public int TotalUnits { get; set; }

        // navigation property that loads all units belonging to this building
        public ICollection<Unit> Units { get; set; } = new List<Unit>();
    }
}
