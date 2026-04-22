using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models
{
    // represents a single rentable unit inside a building

    public class Unit
    {
        [Key]
        public int UnitId { get; set; }

        // foreign key linking this unit to its parent building
        // every unit must belong to a building
        public int BuildingId { get; set; }

        [ForeignKey("BuildingId")]
        public Building Building { get; set; } = null!;

        // the unit label shown in listings and lease forms like 101 or G01
        [Required]
        [MaxLength(50)]
        public string UnitNumber { get; set; } = string.Empty;

        // unit category such as Apartment Office or Shop
        // used to filter available units on the lease application create page
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        // number of bedrooms null for offices and shops
        public int? Bedrooms { get; set; }

        // number of bathrooms null for some commercial units
        public int? Bathrooms { get; set; }

        // floor area in square feet shown on listings and the lease form
        [Column(TypeName = "decimal(18,2)")]
        public decimal SizeInSqFt { get; set; }

        // standard monthly asking rent in Bahraini Dinar
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyRent { get; set; }

        // free text list of amenities stored as a JSON string like Parking Pool Gym
        [MaxLength(1000)]
        public string? Amenities { get; set; }

        // current rental state of this unit
        // valid values are Available Occupied and UnderMaintenance
        [Required]
        [MaxLength(50)]
        public string AvailabilityStatus { get; set; } = "Available";

        // foreign key pointing to the one currently active lease for quick availability lookup
        public int? CurrentLeaseId { get; set; }

        [ForeignKey("CurrentLeaseId")]
        public Lease? CurrentLease { get; set; }

        // full history of every lease ever created for this unit including rejected and terminated ones
        public ICollection<Lease> LeaseHistory { get; set; } = new List<Lease>();

        // all maintenance requests ever submitted for this unit
        public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
    }
}
