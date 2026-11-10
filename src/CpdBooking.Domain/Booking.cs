namespace CpdBooking.Domain;

public sealed class Booking
{
    // Used by EF Core when loading from the database.
    private Booking()
    {
        Email = string.Empty;
        Reference = string.Empty;
    }

    internal Booking(Guid courseId, EmailAddress email, string reference, DateTimeOffset bookedAt)
    {
        Id = Guid.NewGuid();
        CourseId = courseId;
        Email = email.Value;
        Reference = reference;
        BookedAt = bookedAt;
    }

    public Guid Id { get; private set; }

    public Guid CourseId { get; private set; }

    /// <summary>Personal data: never log this value.</summary>
    public string Email { get; private set; }

    /// <summary>Unguessable reference shown to the user. Safe to log.</summary>
    public string Reference { get; private set; }

    public DateTimeOffset BookedAt { get; private set; }
}
