using CpdBooking.Domain;

namespace CpdBooking.Application;

/// <summary>
/// Persistence port. The application depends on this abstraction, and the infrastructure projects
/// implement it (dependency inversion), so storage can change without touching the rules.
/// </summary>
public interface ICourseRepository
{
    Task<IReadOnlyList<Course>> ListStartingAfterAsync(DateTimeOffset from, string? search, CancellationToken cancellationToken);

    Task<Course?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<Course?> FindByBookingReferenceAsync(string reference, CancellationToken cancellationToken);

    /// <exception cref="ConcurrencyConflictException">Someone else changed the course first.</exception>
    /// <exception cref="DuplicateBookingException">The database rejected a duplicate booking.</exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Forget tracked changes so a retry starts from fresh data.</summary>
    void DiscardChanges();
}

public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException()
    {
    }

    public ConcurrencyConflictException(string message)
        : base(message)
    {
    }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public sealed class DuplicateBookingException : Exception
{
    public DuplicateBookingException()
    {
    }

    public DuplicateBookingException(string message)
        : base(message)
    {
    }

    public DuplicateBookingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
