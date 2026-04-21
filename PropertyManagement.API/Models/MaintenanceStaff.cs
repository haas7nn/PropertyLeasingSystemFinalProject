using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.Models
{
    public class MaintenanceStaff : IdentityUser
    {
        [MaxLength(500)]
        public string Skills { get; set; } = "[]";

        [Required]
        [MaxLength(50)]
        public string AvailabilityStatus { get; set; } = "Available";

        // Navigation Properties
        public ICollection<MaintenanceRequest> AssignedRequests { get; set; } = new List<MaintenanceRequest>();
    }
}