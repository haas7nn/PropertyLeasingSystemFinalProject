using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;

namespace PropertyManagement.MVC.Services
{
    // hourly background service that flags overdue payments automatically
    // previously overdue detection only ran when someone visited Payments/Index
    // this service runs independently so statuses are accurate even if no one
    // visits the page which is important for dashboard KPIs and the reporting app
    // uses IServiceScopeFactory to resolve the scoped DbContext from a singleton
    // because direct injection of scoped services into singletons is not allowed
    // by the ASP.NET Core DI container
    public class OverduePaymentService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OverduePaymentService> _logger;
        private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

        public OverduePaymentService(IServiceScopeFactory scopeFactory, ILogger<OverduePaymentService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OverduePaymentService started — runs every hour.");
            while (!stoppingToken.IsCancellationRequested)
            {
                await FlagOverduePaymentsAsync(stoppingToken);
                await Task.Delay(Interval, stoppingToken);
            }
        }

        private async Task FlagOverduePaymentsAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                // find all Pending payments whose due date has passed and update them to Overdue
                var updated = await db.Payments
                    .Where(p => p.Status == "Pending" && p.DueDate < DateTime.UtcNow)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, "Overdue"), ct);
                if (updated > 0)
                    _logger.LogInformation("Flagged {N} payment(s) as Overdue.", updated);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // log the error and keep the service running so it retries next hour
                _logger.LogError(ex, "OverduePaymentService error during check.");
            }
        }
    }
}
