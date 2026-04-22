using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.Models
{
    // role A user type representing a person who rents a unit from the property company

    public class Tenant : IdentityUser
    {
        // Bahrain national ID number used during lease screening
        [MaxLength(20)]
        public string? CPR { get; set; }

        // emergency contact name and phone stored as free text
        [MaxLength(200)]
        public string? EmergencyContact { get; set; }

        // tenants current job used during the lease screening stage
        [MaxLength(200)]
        public string? Occupation { get; set; }

        // date the tenant account was first created in the system
        public DateTime RegistrationDate { get; set; }

        // all lease applications and historical leases belonging to this tenant
        public ICollection<Lease> Leases { get; set; } = new List<Lease>();

        // all maintenance requests submitted by this tenant across all their units
        public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
    }
}
