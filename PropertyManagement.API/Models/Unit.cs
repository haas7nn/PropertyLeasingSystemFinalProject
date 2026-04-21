using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropertyManagement.API.Models
{
    public class Unit
    {
        [Key]
        public int UnitId { get; set; }

        public int BuildingId { get; set; }

        [ForeignKey("BuildingId")]
        public Building Building { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string UnitNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        public int? Bedrooms { get; set; }

        public int? Bathrooms { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SizeInSqFt { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyRent { get; set; }

        [MaxLength(1000)]
        public string? Amenities { get; set; }

        [Required]
        [MaxLength(50)]
        public string AvailabilityStatus { get; set; } = "Available";

        public int? CurrentLeaseId { get; set; }

        [ForeignKey("CurrentLeaseId")]
        public Lease? CurrentLease { get; set; }

        // Navigation Properties
        public ICollection<Lease> LeaseHistory { get; set; } = new List<Lease>();
        public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
    }
}