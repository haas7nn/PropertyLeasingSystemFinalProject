using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.Models
{
    // represents an in-system notification delivered to a user after a business event
    // notifications are created by NotificationService when leases change state
    // payments are recorded or maintenance requests are updated

    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        // identity user ID of the person this notification belongs to
        [Required]
        public string UserId { get; set; } = string.Empty;

        // human readable text shown in the users notification feed
        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        // event category such as LeaseApproved PaymentOverdue or MaintenanceAssigned
        // can be used in future for filtering or icon selection
        [Required]
        [MaxLength(100)]
        public string Type { get; set; } = string.Empty;

        // true once the user has opened the notifications page and seen this entry
        public bool IsRead { get; set; }

        // UTC timestamp of when this notification was created by NotificationService
        public DateTime CreatedDate { get; set; }
    }
}
