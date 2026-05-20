using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;
using PropertyManagement.MVC.Services;

var builder = WebApplication.CreateBuilder(args);

// Restrict controller discovery to the MVC assembly ONLY
// AddApplicationPart() alone is additive it doesn't stop ASP.NET from also scanning
// the referenced API assembly. To truly exclude API [ApiController] classes we must
// use a custom ApplicationPartManager that removes the API assembly before controllers
// are registered
builder.Services.AddControllersWithViews()
    .ConfigureApplicationPartManager(manager =>
    {
        // Remove any parts that came from the API assembly we only want MVC controllers
        // The API assembly is referenced for shared EF models/DbContext, NOT for controllers
        var apiAssembly = typeof(PropertyManagement.API.Controllers.LeasesController).Assembly;
        var partsToRemove = manager.ApplicationParts
            .Where(p => p is Microsoft.AspNetCore.Mvc.ApplicationParts.AssemblyPart ap
                        && ap.Assembly == apiAssembly)
            .ToList();
        foreach (var part in partsToRemove)
            manager.ApplicationParts.Remove(part);
    });

// ===== EF Core - shared DbContext from API project =====
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== ASP.NET Core Identity with cookie-based auth for MVC =====
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Cookie configuration
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// ===== HttpClient: ONLY for PublicLookup page (calls API via HttpClient per rubric) =====
builder.Services.AddHttpClient();
builder.Services.AddScoped<MaintenanceApiService>();
builder.Services.AddScoped<PropertyManagement.API.Services.NotificationService>();
// SignalR hosted in the MVC app so the browser connects same-origin
// no CORS issues regardless of environment, no cross-process WebSocket
// the hub class lives in the API assembly which this project already references
builder.Services.AddSignalR();

var app = builder.Build();

// ── Startup configuration guard ───────────────────────────────────────────────
{
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
    var apiUrl  = builder.Configuration["ApiSettings:BaseUrl"] ?? "";

    if (connStr is "" or "AZURE_OVERRIDE_VIA_ENV_VAR")
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection is not configured. " +
            "Set 'ConnectionStrings__DefaultConnection' in Azure App Service → Configuration.");

    if (apiUrl is "" or "AZURE_OVERRIDE_VIA_ENV_VAR")
    {
        // ApiSettings:BaseUrl is only needed for the PublicLookup page (HttpClient)
        // In local development it defaults to localhost in appsettings.json
        // Log a warning but don't crash other MVC features use EF Core directly
        var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
        startupLogger.LogWarning(
            "ApiSettings:BaseUrl is not configured or is using the sentinel value. " +
            "The PublicLookup feature will not work until this is set.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// NOTE: Database migration is NOT called here
// Only PropertyManagement.API is responsible for running MigrateAsync() + SeedData
// The MVC app shares the same DbContext (EF Core read/write) but must not apply
// migrations independently doing so from two App Services simultaneously risks
// a race condition on the migration history table in Azure SQL
// Deploy the API first; once it is healthy the MVC app can start safely

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Authentication MUST come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// hub is same-origin so no RequireCors needed
// the MVC cookie auth is NOT applied here because the board page
// already enforces [Authorize] before the browser ever loads the JS that connects
app.MapHub<PropertyManagement.API.Hubs.MaintenanceHub>("/hubs/maintenance");

app.Run();
