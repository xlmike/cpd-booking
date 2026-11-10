using System.Collections.Concurrent;
using CpdBooking.Application;
using CpdBooking.Domain;

namespace CpdBooking.Infrastructure;

/// <summary>
/// For local development and automated tests. Data is lost when the app stops.
/// Course.Book is thread-safe, so concurrent bookings on one course can't overbook.
/// </summary>
public sealed class InMemoryCourseRepository : ICourseRepository
{
    private readonly ConcurrentDictionary<Guid, Course> _courses = new();

    public InMemoryCourseRepository(TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        foreach (var course in SeedData.Courses(clock.GetUtcNow()))
        {
            _courses[course.Id] = course;
        }
    }

    public Task<IReadOnlyList<Course>> ListStartingAfterAsync(DateTimeOffset from, string? search, CancellationToken cancellationToken)
    {
        IReadOnlyList<Course> result = _courses.Values
            .Where(c => c.StartsAt > from)
            .Where(c => search is null
                || c.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                || c.Provider.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult(result);
    }

    public Task<Course?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_courses.GetValueOrDefault(id));

    public Task<Course?> FindByBookingReferenceAsync(string reference, CancellationToken cancellationToken) =>
        Task.FromResult(_courses.Values.FirstOrDefault(c => c.Bookings.Any(b => b.Reference == reference)));

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void DiscardChanges()
    {
        // Nothing to discard: changes are applied directly to the in-memory objects.
    }
}
