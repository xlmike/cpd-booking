using CpdBooking.Application;
using CpdBooking.Infrastructure;

namespace CpdBooking.UnitTests;

public class CourseCatalogueTests
{
    private readonly FixedTimeProvider _clock = new(Courses.Now);

    [Fact]
    public async Task Upcoming_courses_are_listed_soonest_first()
    {
        var catalogue = new CourseCatalogue(new InMemoryCourseRepository(_clock), _clock);

        var courses = await catalogue.ListUpcomingAsync(null, CancellationToken.None);

        Assert.Equal(4, courses.Count);
        Assert.Equal(courses.OrderBy(c => c.StartsAt), courses);
    }

    [Fact]
    public async Task Past_courses_are_not_listed()
    {
        var repository = new InMemoryCourseRepository(_clock);
        _clock.Now = Courses.Now.AddDays(20);

        var courses = await new CourseCatalogue(repository, _clock).ListUpcomingAsync(null, CancellationToken.None);

        Assert.All(courses, c => Assert.True(c.StartsAt > _clock.Now));
    }

    [Theory]
    [InlineData("PHONICS", 1)]
    [InlineData("  computing ", 1)]
    [InlineData("Midlands", 1)]
    [InlineData("nothing matches this", 0)]
    public async Task Search_matches_title_or_provider_ignoring_case_and_spaces(string search, int expected)
    {
        var catalogue = new CourseCatalogue(new InMemoryCourseRepository(_clock), _clock);

        var courses = await catalogue.ListUpcomingAsync(search, CancellationToken.None);

        Assert.Equal(expected, courses.Count);
    }

    [Fact]
    public async Task An_unknown_booking_reference_has_no_confirmation()
    {
        var catalogue = new CourseCatalogue(new InMemoryCourseRepository(_clock), _clock);

        Assert.Null(await catalogue.GetConfirmationAsync("CPD-NOPE-NOPE", CancellationToken.None));
    }
}
