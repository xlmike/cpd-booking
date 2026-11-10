using System.Threading.RateLimiting;
using CpdBooking.Application;
using CpdBooking.Infrastructure;
using CpdBooking.Infrastructure.SqlServer;
using CpdBooking.Web.Security;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new ResponseCacheAttribute { NoStore = true, Location = ResponseCacheLocation.None }));
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

builder.Services.AddApplication();
builder.Services.AddCoreInfrastructure();

var persistence = builder.Configuration["Persistence:Provider"] ?? "InMemory";
if (string.Equals(persistence, "SqlServer", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSqlServerPersistence(builder.Configuration);
}
else
{
    builder.Services.AddInMemoryPersistence();
}

// A01/A07: slow down automated booking abuse. 10 bookings a minute per client IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimitPolicies.Bookings, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "cpd-antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsProduction() ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

// App Service terminates TLS at its front end and forwards the original scheme and client IP.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    // A02/A10: never show stack traces to users; return a friendly page or ProblemDetails.
    app.UseExceptionHandler("/error");
    app.UseHsts();
    app.UseHttpsRedirection(); // App Service also enforces HTTPS (https_only in Terraform)
}

app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();

app.MapHealthChecks("/health");
app.MapControllers();
app.MapControllerRoute(name: "default", pattern: "{controller=Courses}/{action=Index}/{id?}");

if (string.Equals(persistence, "SqlServer", StringComparison.OrdinalIgnoreCase))
{
    var migrate = app.Configuration.GetValue("Persistence:MigrateOnStartup", false);
    await app.Services.InitialiseSqlServerAsync(migrate, app.Services.GetRequiredService<TimeProvider>());
}

await app.RunAsync();

/// <summary>Exposed so integration tests can start the app with WebApplicationFactory.</summary>
public partial class Program;
