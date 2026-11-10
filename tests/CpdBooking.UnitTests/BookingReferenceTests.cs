using CpdBooking.Infrastructure;

namespace CpdBooking.UnitTests;

public class BookingReferenceTests
{
    [Fact]
    public void References_have_the_expected_format_and_avoid_confusing_characters()
    {
        var reference = new RandomBookingReferenceGenerator().Create();

        Assert.Matches("^CPD-[2-9A-HJKMNP-Z]{4}-[2-9A-HJKMNP-Z]{4}$", reference);
    }

    [Fact]
    public void References_are_not_repeated()
    {
        var generator = new RandomBookingReferenceGenerator();

        var references = Enumerable.Range(0, 10_000).Select(_ => generator.Create()).ToHashSet();

        Assert.Equal(10_000, references.Count);
    }
}
