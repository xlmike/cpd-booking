using CpdBooking.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CpdBooking.Infrastructure.SqlServer;

public sealed class CpdBookingDbContext(DbContextOptions<CpdBookingDbContext> options) : DbContext(options)
{
    public DbSet<Course> Courses => Set<Course>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Course>(course =>
        {
            course.HasKey(c => c.Id);
            course.Property(c => c.Title).HasMaxLength(200).IsRequired();
            course.Property(c => c.Provider).HasMaxLength(200).IsRequired();
            course.Property(c => c.Summary).HasMaxLength(1000);
            course.Ignore(c => c.PlacesLeft);

            // Optimistic concurrency: a shadow row version, so the domain stays free of database concerns.
            course.Property<byte[]>("RowVersion").IsRowVersion();

            course.HasMany(c => c.Bookings).WithOne().HasForeignKey(b => b.CourseId);
            course.Navigation(c => c.Bookings).UsePropertyAccessMode(PropertyAccessMode.Field);
            course.HasIndex(c => c.StartsAt);
        });

        modelBuilder.Entity<Booking>(booking =>
        {
            booking.HasKey(b => b.Id);
            booking.Property(b => b.Email).HasMaxLength(EmailAddress.MaxLength).IsRequired();
            booking.Property(b => b.Reference).HasMaxLength(20).IsRequired();

            // Defence in depth: the database also refuses double bookings and duplicate references.
            booking.HasIndex(b => new { b.CourseId, b.Email }).IsUnique();
            booking.HasIndex(b => b.Reference).IsUnique();
        });
    }
}

/// <summary>Lets `dotnet ef migrations add` create the context without running the web app.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CpdBookingDbContext>
{
    public CpdBookingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CpdBookingDbContext>()
            .UseSqlServer("Server=(localdb)\\design-time-only;Database=CpdBooking;Trusted_Connection=True")
            .Options;
        return new CpdBookingDbContext(options);
    }
}
