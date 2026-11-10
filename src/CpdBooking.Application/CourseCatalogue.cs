namespace CpdBooking.Application;

/// <summary>Read side: finding and viewing courses. Kept separate from booking (single responsibility).</summary>
public sealed class CourseCatalogue(ICourseRepository courses, TimeProvider clock)
{
    public const int MaxSearchLength = 100;

    public async Task<IReadOnlyList<CourseSummary>> ListUpcomingAsync(string? search, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var term = Normalise(search);
        var results = await courses.ListStartingAfterAsync(now, term, cancellationToken).ConfigureAwait(false);
        return results
            .OrderBy(c => c.StartsAt)
            .Select(c => CourseSummary.From(c, now))
            .ToList();
    }

    public async Task<CourseSummary?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var course = await courses.FindAsync(id, cancellationToken).ConfigureAwait(false);
        return course is null ? null : CourseSummary.From(course, clock.GetUtcNow());
    }

    public async Task<BookingConfirmation?> GetConfirmationAsync(string reference, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.Length > 20)
        {
            return null;
        }

        var course = await courses.FindByBookingReferenceAsync(reference, cancellationToken).ConfigureAwait(false);
        return course is null ? null : new BookingConfirmation(reference, course.Id, course.Title, course.StartsAt);
    }

    private static string? Normalise(string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return null;
        }

        var trimmed = search.Trim();
        return trimmed.Length > MaxSearchLength ? trimmed[..MaxSearchLength] : trimmed;
    }
}
