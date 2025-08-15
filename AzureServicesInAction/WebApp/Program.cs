using Microsoft.ApplicationInsights;
using WebApp.Services.MetricsLogger;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();


//APPINSIGHTS_INSTRUMENTATIONKEY is set in the Azure portal for Application Insights integration.
builder.Services.AddApplicationInsightsTelemetry();


// Telemetry__EnableLogging = false/true in Azure App service to enable/disable logging/tracking
builder.Services.Configure<TelemetryOptions>(
    builder.Configuration.GetSection("Telemetry"));

builder.Services.AddSingleton<IMetricsLogger, MetricsLogger>();
builder.Services.AddSingleton<TelemetryClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapGet("/Home", (IConfiguration config) =>
{
    var tenantId = config["AzureAd:TenantId"];
    var clientId = config["AzureAd:ClientId"];
    var clientSecret = config["AzureAd:ClientSecret"];

    return $"TenantId: {tenantId}, ClientId: {clientId}, ClientSecret: {clientSecret}";
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
