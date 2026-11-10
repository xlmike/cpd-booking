using Microsoft.Extensions.DependencyInjection;

namespace CpdBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<CourseCatalogue>();
        services.AddScoped<BookingService>();
        return services;
    }
}
