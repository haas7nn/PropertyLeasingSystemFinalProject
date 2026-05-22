using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.MVC.Models
{
    public class LoginViewModel
    {
        [Required][EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required][DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required][EmailAddress][Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required][StringLength(100, ErrorMessage = "Password must be at least {2} characters.", MinimumLength = 6)]
        [DataType(DataType.Password)][Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)][Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Phone][Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [MaxLength(20)][Display(Name = "CPR Number")]
        public string? CPR { get; set; }

        [MaxLength(200)][Display(Name = "Occupation")]
        public string? Occupation { get; set; }

        [MaxLength(200)][Display(Name = "Emergency Contact")]
        public string? EmergencyContact { get; set; }
    }

    public class MaintenanceRequestViewModel
    {
        public int RequestId { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedDate { get; set; }
        public string? AssignedStaffName { get; set; }
        public string? ResolutionNotes { get; set; }
        public DateTime? ClosedDate { get; set; }
        public string? UnitNumber { get; set; }
    }

    /// <summary>Extended view model used by the manager/staff Details page (assign + update status)</summary>
    public class MaintenanceDetailViewModel : MaintenanceRequestViewModel
    {
        public string TenantEmail { get; set; } = string.Empty;
        public string TenantPhone { get; set; } = string.Empty;
        public string BuildingName { get; set; } = string.Empty;
        public string? AssignedStaffId { get; set; }
        public List<StaffSelectItem> AvailableStaff { get; set; } = new();
    }

    public class StaffSelectItem
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Skills { get; set; } = string.Empty;
        public string AvailabilityStatus { get; set; } = string.Empty;
    }

    public class SubmitMaintenanceViewModel
    {
        [Required][Display(Name = "Unit")]
        public int UnitId { get; set; }

        [Required][Display(Name = "Category")]
        public string Category { get; set; } = string.Empty;

        [Required][Display(Name = "Priority")]
        public string Priority { get; set; } = string.Empty;

        [Required][MinLength(10, ErrorMessage = "Please provide at least 10 characters describing the issue.")]
        [MaxLength(2000)][Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;
    }

    // ── Staff Management ────────────────────────────────────────
    public class StaffViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string Skills { get; set; } = string.Empty;
        public string AvailabilityStatus { get; set; } = string.Empty;
        public int ActiveRequests { get; set; }
    }

    public class CreateStaffViewModel
    {
        [Required][EmailAddress][Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required][StringLength(100, MinimumLength = 6)][DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)][Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Phone][Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Skills (comma-separated e.g. Plumbing, Electrical)")]
        public string Skills { get; set; } = string.Empty;
    }

    public class EditStaffViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required][EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone][Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Skills (comma-separated)")]
        public string Skills { get; set; } = string.Empty;

        [Required][Display(Name = "Availability Status")]
        public string AvailabilityStatus { get; set; } = "Available";
    }

    // ── Notification ────────────────────────────────────────────
    public class NotificationViewModel
    {
        public int NotificationId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
