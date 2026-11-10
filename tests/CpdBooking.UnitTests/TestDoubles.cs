using CpdBooking.Application;
using CpdBooking.Domain;

namespace CpdBooking.UnitTests;

/// <summary>A clock we control, so time-based rules are tested exactly.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

/// <summary>Predictable references: REF-1, REF-2...</summary>
internal sealed class SequentialReferences : IBookingReferenceGenerator
{
    private int _next;

    public string Create() => $"REF-{++_next}";
}

/// <summary>
/// A hand-written fake repository that behaves like EF Core: after DiscardChanges, the next load
/// returns a fresh copy of the course, without the failed attempt's changes.
/// </summary>
internal sealed class FakeCourseRepository : ICourseRepository
{
    private readonly Dictionary<Guid, Func<Course>> _stored = [];
    private readonly Dictionary<Guid, Course> _loaded = [];

    public FakeCourseRepository(params Func<Course>[] courses)
    {
        foreach (var create in courses)
        {
            _stored[create().Id] = create;
        }
    }

    public int SaveCount { get; private set; }

    public int DiscardCount { get; private set; }

    public Queue<Exception> SaveFailures { get; } = new();

    public Task<IReadOnlyList<Course>> ListStartingAfterAsync(DateTimeOffset from, string? search, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Course>>(_stored.Keys.Select(Load).Where(c => c.StartsAt > from).ToList());

    public Task<Course?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_stored.ContainsKey(id) ? Load(id) : null);

    public Task<Course?> FindByBookingReferenceAsync(string reference, CancellationToken cancellationToken) =>
        Task.FromResult(_loaded.Values.FirstOrDefault(c => c.Bookings.Any(b => b.Reference == reference)));

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return SaveFailures.TryDequeue(out var failure) ? Task.FromException(failure) : Task.CompletedTask;
    }

    public void DiscardChanges()
    {
        DiscardCount++;
        _loaded.Clear();
    }

    private Course Load(Guid id)
    {
        if (!_loaded.TryGetValue(id, out var course))
        {
            course = _stored[id]();
            _loaded[id] = course;
        }

        return course;
    }
}

internal static class Courses
{
    public static readonly DateTimeOffset Now = new(2026, 11, 9, 10, 0, 0, TimeSpan.Zero);

    public static readonly Guid PhonicsId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static Course StartingIn(TimeSpan fromNow, int capacity = 10) =>
        new(Guid.NewGuid(), "Phonics", "Hub", "Summary", Now + fromNow, capacity);

    /// <summary>A factory for the fake repository: the same course (same id) each time it's loaded.</summary>
    public static Func<Course> Phonics(TimeSpan startsIn, int capacity = 10) =>
        () => new Course(PhonicsId, "Phonics", "Hub", "Summary", Now + startsIn, capacity);
}
