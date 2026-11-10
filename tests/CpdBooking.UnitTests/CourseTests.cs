using CpdBooking.Domain;

namespace CpdBooking.UnitTests;

/// <summary>
/// The booking rules, written test-first. Each test name reads as a rule a product manager would recognise.
/// </summary>
public class CourseTests
{
    private static readonly DateTimeOffset Now = Courses.Now;
    private readonly SequentialReferences _references = new();

    [Fact]
    public void Booking_a_course_with_places_returns_a_booking_with_a_reference()
    {
        var course = Courses.StartingIn(TimeSpan.FromDays(7));

        var result = course.Book("teacher@school.org.uk", Now, _references);

        Assert.True(result.IsSuccess);
        Assert.Equal("REF-1", result.Booking!.Reference);
        Assert.Equal(course.Id, result.Booking.CourseId);
        Assert.Equal(Now, result.Booking.BookedAt);
    }

    [Fact]
    public void Booking_reduces_the_places_left()
    {
        var course = Courses.StartingIn(TimeSpan.FromDays(7), capacity: 3);

        course.Book("a@school.org.uk", Now, _references);

        Assert.Equal(2, course.PlacesLeft);
    }

    [Fact]
    public void A_missing_email_is_rejected()
    {
        var course = Courses.StartingIn(TimeSpan.FromDays(7));
        var result = course.Book(null, Now, _references);
        Assert.Equal(BookingError.InvalidEmail, result.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("two@@school.org.uk")]
    [InlineData("no-domain@")]
    [InlineData("has space@school.org.uk")]
    [InlineData("teacher@localhost")]
    public void An_invalid_email_is_rejected(string email)
    {
        var course = Courses.StartingIn(TimeSpan.FromDays(7));
        var result = course.Book(email, Now, _references);
        Assert.Equal(BookingError.InvalidEmail, result.Error);
        Assert.Empty(course.Bookings);
    }

    [Fact]
    public void A_full_course_cannot_be_booked()
    {
        var course = Courses.StartingIn(TimeSpan.FromDays(7), capacity: 1);
        course.Book("first@school.org.uk", Now, _references);

        var result = course.Book("second@school.org.uk", Now, _references);

        Assert.Equal(BookingError.CourseFull, result.Error);
        Assert.Single(course.Bookings);
    }

    [Fact]
    public void Bookings_close_48_hours_before_the_start()
    {
        var course = Courses.StartingIn(Course.BookingCutOff);

        var result = course.Book("teacher@school.org.uk", Now, _references);

        Assert.Equal(BookingError.BookingClosed, result.Error);
    }


    [Fact]
    public void Booking_one_minute_before_the_cut_off_succeeds()
    {
        var course = Courses.StartingIn(Course.BookingCutOff + TimeSpan.FromMinutes(1));

        var result = course.Book("teacher@school.org.uk", Now, _references);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("TEACHER@school.org.uk")]
    [InlineData("  teacher@school.org.uk  ")]
    public void The_same_email_cannot_book_twice_whatever_its_case_or_spacing(string secondAttempt)
    {
        var course = Courses.StartingIn(TimeSpan.FromDays(7));
        course.Book("teacher@school.org.uk", Now, _references);

        var result = course.Book(secondAttempt, Now, _references);

        Assert.Equal(BookingError.AlreadyBooked, result.Error);
    }

    [Fact]
    public void Concurrent_bookings_never_exceed_capacity()
    {
        var course = Courses.StartingIn(TimeSpan.FromDays(7), capacity: 5);
        var references = new SequentialReferences();

        Parallel.For(0, 200, i => course.Book($"teacher{i}@school.org.uk", Now, references));

        Assert.Equal(5, course.Bookings.Count);
        Assert.Equal(0, course.PlacesLeft);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_course_must_have_at_least_one_place(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new Course(Guid.NewGuid(), "Title", "Provider", "Summary", Now, capacity));
    }

    [Fact]
    public void A_course_is_not_open_for_booking_when_full()
    {
        var course = Courses.StartingIn(TimeSpan.FromDays(7), capacity: 1);
        course.Book("a@school.org.uk", Now, _references);

        Assert.False(course.IsOpenForBooking(Now));
    }


}
