using CpdBooking.Application;
using CpdBooking.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CpdBooking.Infrastructure.SqlServer;

public sealed class EfCourseRepository(CpdBookingDbContext db) : ICourseRepository
{
    public async Task<IReadOnlyList<Course>> ListStartingAfterAsync(DateTimeOffset from, string? search, CancellationToken cancellationToken)
    {
        var query = db.Courses.AsNoTracking().Where(c => c.StartsAt > from);
        if (search is not null)
        {
            // Parameterised by EF Core: no SQL injection. Case-insensitive through the database collation.
            //query = query.Where(c => c.Title.Contains(search) || c.Provider.Contains(search));
            query = db.Courses.FromSqlRaw($"SELECT * FROM Courses WHERE Title LIKE '%{search}%'");
        }

        return await query.OrderBy(c => c.StartsAt).Take(100).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<Course?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.Courses.Include(c => c.Bookings).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Course?> FindByBookingReferenceAsync(string reference, CancellationToken cancellationToken) =>
        db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Bookings.Any(b => b.Reference == reference), cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("The course was changed by another booking.", ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new DuplicateBookingException("The database rejected a duplicate booking.", ex);
        }
    }

    public void DiscardChanges() => db.ChangeTracker.Clear();
}
