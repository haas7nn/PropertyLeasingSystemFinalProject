using PropertyManagement.MVC.Services;
using Microsoft.EntityFrameworkCore;
using PropertyManagement.API.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllersWithViews();

// Register HttpClient
builder.Services.AddHttpClient();

// Register API Service
builder.Services.AddScoped<MaintenanceApiService>();
builder.Services.AddScoped<PropertyApiService>();
// Program.cs (MVC Project)

builder.Services.AddHttpClient<LeaseApiService>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7168/");

    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddHttpClient<PaymentApiService>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7168/");
});
var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();