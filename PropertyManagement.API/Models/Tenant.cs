using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.Models
{
    public class Tenant : IdentityUser
    {
        [MaxLength(20)]
        public string? CPR { get; set; }

        [MaxLength(200)]
        public string? EmergencyContact { get; set; }

        [MaxLength(200)]
        public string? Occupation { get; set; }

        public DateTime RegistrationDate { get; set; }

        // Navigation Properties
        public ICollection<Lease> Leases { get; set; } = new List<Lease>();
        public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
    }
}