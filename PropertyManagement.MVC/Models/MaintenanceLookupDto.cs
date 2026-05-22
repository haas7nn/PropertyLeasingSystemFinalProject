namespace PropertyManagement.MVC.Models
{
    // DTO returned by MaintenanceApiService after calling the API public lookup endpoint
    // maps directly to the anonymous object the API returns from the lookup action
    public class MaintenanceLookupDto
    {
        public string TicketNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime SubmittedDate { get; set; }
        public string Building { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string? AssignedStaff { get; set; }
        public string? ResolutionNotes { get; set; }
        public DateTime? ClosedDate { get; set; }

        // set by MaintenanceApiService when the API is completely unreachable
        // lets the controller distinguish a not found result from a service outage
        // without needing separate null checks for each case in the view
        public bool ApiDown { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
