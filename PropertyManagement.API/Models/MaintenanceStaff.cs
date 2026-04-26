using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.Models
{
    // role B user type representing a maintenance technician
    public class MaintenanceStaff : IdentityUser
    {
        // JSON serialised list of skill categories this staff member can handle
        // example value is ["Plumbing","Electrical"]
        [MaxLength(500)]
        public string Skills { get; set; } = "[]";

        // whether this staff member is currently available for assignment
        // valid values are Available and Unavailable
        [Required]
        [MaxLength(50)]
        public string AvailabilityStatus { get; set; } = "Available";

        // all maintenance requests currently or previously assigned to this staff member
        public ICollection<MaintenanceRequest> AssignedRequests { get; set; } = new List<MaintenanceRequest>();
    }
}
