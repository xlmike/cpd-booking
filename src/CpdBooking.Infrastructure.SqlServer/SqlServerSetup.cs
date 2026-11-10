using CpdBooking.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CpdBooking.Infrastructure.SqlServer;

public static class SqlServerSetup
{
    /// <summary>
    /// The connection string has no password. In Azure it uses
    /// "Authentication=Active Directory Default", so the App Service managed identity signs in.
    /// </summary>
    public static IServiceCollection AddSqlServerPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CpdBooking")
            ?? throw new InvalidOperationException("ConnectionStrings:CpdBooking is not set.");

        services.AddDbContext<CpdBookingDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 5)));
        services.AddScoped<ICourseRepository, EfCourseRepository>();
        return services;
    }

    /// <summary>Applies migrations and adds example courses if the database is empty.</summary>
    public static async Task InitialiseSqlServerAsync(this IServiceProvider services, bool migrate, TimeProvider clock, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(clock);

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CpdBookingDbContext>();
        if (migrate)
        {
            await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!await db.Courses.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            db.Courses.AddRange(SeedData.Courses(clock.GetUtcNow()));
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
