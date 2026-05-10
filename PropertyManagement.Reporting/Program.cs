using PropertyManagement.Reporting.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();   // needed by ApiService to read session token
builder.Services.AddScoped<ApiService>();
// Session stores the JWT token returned from the API login step
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ── Startup configuration guard ───────────────────────────────────────────────
{
    var apiUrl  = builder.Configuration["ApiSettings:BaseUrl"]       ?? "";
    var email   = builder.Configuration["ApiSettings:ManagerEmail"]  ?? "";
    var pwd     = builder.Configuration["ApiSettings:ManagerPassword"] ?? "";

    if (apiUrl is "" or "AZURE_OVERRIDE_VIA_ENV_VAR")
        throw new InvalidOperationException(
            "ApiSettings:BaseUrl is not configured. " +
            "Set 'ApiSettings__BaseUrl' in Azure App Service → Configuration.");

    if (email is "" or "AZURE_OVERRIDE_VIA_ENV_VAR" || pwd is "" or "AZURE_OVERRIDE_VIA_ENV_VAR")
        throw new InvalidOperationException(
            "ApiSettings:ManagerEmail / ManagerPassword is not configured. " +
            "Set 'ApiSettings__ManagerEmail' and 'ApiSettings__ManagerPassword' " +
            "in Azure App Service → Configuration.");
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();  // Must come before UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Reports}/{action=Dashboard}/{id?}");

app.Run();
