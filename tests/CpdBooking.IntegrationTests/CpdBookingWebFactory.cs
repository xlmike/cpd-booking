using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CpdBooking.IntegrationTests;

/// <summary>Starts the real app in memory, with in-memory data, for each test class.</summary>
public sealed class CpdBookingWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Persistence:Provider", "InMemory");
    }
}
