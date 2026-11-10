namespace CpdBooking.Domain;

/// <summary>
/// A training course. It is the aggregate root for its bookings, so every booking rule lives here.
/// </summary>
public sealed class Course
{
    /// <summary>Bookings close this long before the course starts.</summary>
    public static readonly TimeSpan BookingCutOff = TimeSpan.FromHours(48);
    private readonly List<Booking> _bookings = [];
    private readonly object _sync = new();

    // Used by EF Core when loading from the database.
    private Course()
    {
        Title = string.Empty;
        Provider = string.Empty;
        Summary = string.Empty;
    }

    public Course(Guid id, string title, string provider, string summary, DateTimeOffset startsAt, int capacity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);

        Id = id;
        Title = title.Trim();
        Provider = provider.Trim();
        Summary = summary?.Trim() ?? string.Empty;
        StartsAt = startsAt;
        Capacity = capacity;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; }

    public string Provider { get; private set; }

    public string Summary { get; private set; }

    public DateTimeOffset StartsAt { get; private set; }

    public int Capacity { get; private set; }

    /// <summary>
    /// Stored on the course row so that every booking updates the course. With a row version,
    /// two people booking the last place at the same moment cause a concurrency conflict
    /// instead of an overbooking.
    /// </summary>
    public int BookedPlaces { get; private set; }

    public IReadOnlyCollection<Booking> Bookings => _bookings.AsReadOnly();

    public int PlacesLeft => Capacity - BookedPlaces;

    public bool IsOpenForBooking(DateTimeOffset now) => now < StartsAt - BookingCutOff && PlacesLeft > 0;

    public BookingResult Book(string? email, DateTimeOffset now, IBookingReferenceGenerator references)
    {
        ArgumentNullException.ThrowIfNull(references);

        if (!EmailAddress.TryCreate(email, out var address) || address is null)
        {
            return BookingResult.Failure(BookingError.InvalidEmail);
        }

        lock (_sync) {
            if (now >= StartsAt - BookingCutOff)
            {
                return BookingResult.Failure(BookingError.BookingClosed);
            }


            if (PlacesLeft <= 0)
            {
                return BookingResult.Failure(BookingError.CourseFull);
            }

            if (_bookings.Exists(b => b.Email == address.Value))
            {
                return BookingResult.Failure(BookingError.AlreadyBooked);
            }


            var booking = new Booking(Id, address, references.Create(), now);
            _bookings.Add(booking);
            BookedPlaces++;
            return BookingResult.Success(booking);

        }
    }
}
