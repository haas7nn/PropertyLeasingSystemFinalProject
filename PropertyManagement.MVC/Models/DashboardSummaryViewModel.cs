namespace PropertyManagement.MVC.Models
{
    // view model for the Home dashboard page
    // populated by HomeController using EF Core queries directly against the shared DbContext
    // authenticated users see all eight KPI counters
    // guests only see the four public hero counters shown in the landing page banner
    public class DashboardSummaryViewModel
    {
        // total number of buildings in the system
        public int TotalBuildings { get; set; }

        // total number of units across all buildings
        public int TotalUnits { get; set; }

        // number of units currently occupied by an active tenant
        public int OccupiedUnits { get; set; }

        // number of units currently available for lease
        public int AvailableUnits { get; set; }

        // number of leases with status Active
        public int ActiveLeases { get; set; }

        // number of leases in Application or Screening status waiting for manager action
        public int PendingApplications { get; set; }

        // number of maintenance requests that are not yet Closed
        public int OpenMaintenanceRequests { get; set; }

        // number of payments already flagged as Overdue by OverduePaymentService
        public int OverduePayments { get; set; }

        // computed occupancy percentage rounded to one decimal place
        // returns 0 when there are no units to avoid a divide-by-zero error
        public double OccupancyRate => TotalUnits > 0
            ? Math.Round((double)OccupiedUnits / TotalUnits * 100, 1)
            : 0;
    }
}
