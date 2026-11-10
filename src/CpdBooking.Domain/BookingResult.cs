namespace CpdBooking.Domain;

public enum BookingError
{
    None = 0,
    CourseNotFound,
    InvalidEmail,
    BookingClosed,
    CourseFull,
    AlreadyBooked,
}

/// <summary>
/// The outcome of trying to book. Expected business failures are results, not exceptions.
/// </summary>
public sealed record BookingResult
{
    private BookingResult(Booking? booking, BookingError error)
    {
        Booking = booking;
        Error = error;
    }

    public Booking? Booking { get; }

    public BookingError Error { get; }

    public bool IsSuccess => Error == BookingError.None;

    public static BookingResult Success(Booking booking) => new(booking, BookingError.None);

    public static BookingResult Failure(BookingError error) =>
        error == BookingError.None
            ? throw new ArgumentException("A failure needs an error.", nameof(error))
            : new BookingResult(null, error);
}
