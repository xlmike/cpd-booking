using CpdBooking.Application;
using CpdBooking.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace CpdBooking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IBookingReferenceGenerator, RandomBookingReferenceGenerator>();
        return services;
    }

    public static IServiceCollection AddInMemoryPersistence(this IServiceCollection services)
    {
        services.AddSingleton<ICourseRepository, InMemoryCourseRepository>();
        return services;
    }
}
