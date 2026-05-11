namespace PropertyManagement.Reporting.Models
{
    // Auth
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public DateTime Expiration { get; set; }
    }

    // Occupancy Report
    public class OccupancyReport
    {
        public OccupancySummary Summary { get; set; } = new();
        public List<BuildingOccupancy> ByBuilding { get; set; } = new();
    }

    public class OccupancySummary
    {
        public int TotalUnits { get; set; }
        public int OccupiedUnits { get; set; }
        public int AvailableUnits { get; set; }
        public int UnderMaintenance { get; set; }
        public double OccupancyRate { get; set; }
    }

    public class BuildingOccupancy
    {
        public string BuildingName { get; set; } = string.Empty;
        public string? Location { get; set; }
        public int TotalUnits { get; set; }
        public int Occupied { get; set; }
        public int Available { get; set; }
        public double OccupancyRate { get; set; }
    }

    // Maintenance Stats Report
    public class MaintenanceStatsReport
    {
        public int TotalRequests { get; set; }
        public List<StatusCount> ByStatus { get; set; } = new();
        public List<CategoryCount> ByCategory { get; set; } = new();
        public List<PriorityCount> ByPriority { get; set; } = new();
        public double AverageResolutionDays { get; set; }
    }

    public class StatusCount { public string Status { get; set; } = string.Empty; public int Count { get; set; } }
    public class CategoryCount { public string Category { get; set; } = string.Empty; public int Count { get; set; } }
    public class PriorityCount { public string Priority { get; set; } = string.Empty; public int Count { get; set; } }

    // Payment Summary Report
    public class PaymentSummaryReport
    {
        public decimal TotalDue { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalPending { get; set; }
        public decimal TotalOverdue { get; set; }
        public int OverdueCount { get; set; }
        public double CollectionRate { get; set; }
    }

    // Building list item (for overview)
    public class BuildingReportItem
    {
        public int BuildingId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Location { get; set; }
        public int TotalUnits { get; set; }
        public int AvailableUnits { get; set; }
        public int OccupiedUnits { get; set; }
    }

    // Dashboard view model
    public class DashboardViewModel
    {
        public OccupancyReport? Occupancy { get; set; }
        public MaintenanceStatsReport? MaintenanceStats { get; set; }
        public PaymentSummaryReport? PaymentSummary { get; set; }
        public bool ApiConnected { get; set; }
    }
}
