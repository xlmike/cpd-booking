using CpdBooking.Domain;

namespace CpdBooking.Application;

/// <summary>
/// What the outside world sees of a course. A DTO, so the domain model (and its bookings,
/// which hold personal data) never leaks through the API or views.
/// </summary>
public sealed record CourseSummary(
    Guid Id,
    string Title,
    string Provider,
    string Summary,
    DateTimeOffset StartsAt,
    int PlacesLeft,
    bool IsOpenForBooking)
{
    public static CourseSummary From(Course course, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(course);
        return new(
            course.Id,
            course.Title,
            course.Provider,
            course.Summary,
            course.StartsAt,
            course.PlacesLeft,
            course.IsOpenForBooking(now));
    }
}

public sealed record BookingConfirmation(string Reference, Guid CourseId, string CourseTitle, DateTimeOffset StartsAt);
