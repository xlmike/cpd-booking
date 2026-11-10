using CpdBooking.Application;
using CpdBooking.Domain;
using Microsoft.Extensions.Logging.Abstractions;

namespace CpdBooking.UnitTests;

public class BookingServiceTests
{
    private static readonly Guid CourseId = Courses.PhonicsId;
    private readonly FixedTimeProvider _clock = new(Courses.Now);

    private BookingService CreateService(FakeCourseRepository repository) =>
        new(repository, new SequentialReferences(), _clock, NullLogger<BookingService>.Instance);

    [Fact]
    public async Task Booking_an_unknown_course_returns_course_not_found()
    {
        var repository = new FakeCourseRepository();

        var result = await CreateService(repository).BookAsync(Guid.NewGuid(), "a@school.org.uk", CancellationToken.None);

        Assert.Equal(BookingError.CourseNotFound, result.Error);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task A_successful_booking_is_saved_once()
    {
        var repository = new FakeCourseRepository(Courses.Phonics(TimeSpan.FromDays(7)));

        var result = await CreateService(repository).BookAsync(CourseId, "a@school.org.uk", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task A_rejected_booking_is_not_saved()
    {
        var repository = new FakeCourseRepository(Courses.Phonics(TimeSpan.FromHours(1)));

        var result = await CreateService(repository).BookAsync(CourseId, "a@school.org.uk", CancellationToken.None);

        Assert.Equal(BookingError.BookingClosed, result.Error);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task A_concurrency_conflict_is_retried_with_fresh_data()
    {
        var repository = new FakeCourseRepository(Courses.Phonics(TimeSpan.FromDays(7)));
        repository.SaveFailures.Enqueue(new ConcurrencyConflictException());

        var result = await CreateService(repository).BookAsync(CourseId, "a@school.org.uk", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, repository.SaveCount);
        Assert.Equal(1, repository.DiscardCount);
    }

    [Fact]
    public async Task Repeated_concurrency_conflicts_give_up_after_the_maximum_attempts()
    {
        var repository = new FakeCourseRepository(Courses.Phonics(TimeSpan.FromDays(7)));
        for (var i = 0; i < BookingService.MaxAttempts; i++)
        {
            repository.SaveFailures.Enqueue(new ConcurrencyConflictException());
        }

        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => CreateService(repository).BookAsync(CourseId, "a@school.org.uk", CancellationToken.None));
        Assert.Equal(BookingService.MaxAttempts, repository.SaveCount);
    }

    [Fact]
    public async Task A_duplicate_rejected_by_the_database_is_reported_as_already_booked()
    {
        var repository = new FakeCourseRepository(Courses.Phonics(TimeSpan.FromDays(7)));
        repository.SaveFailures.Enqueue(new DuplicateBookingException());

        var result = await CreateService(repository).BookAsync(CourseId, "a@school.org.uk", CancellationToken.None);

        Assert.Equal(BookingError.AlreadyBooked, result.Error);
        Assert.Equal(1, repository.DiscardCount);
    }
}
