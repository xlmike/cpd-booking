using CpdBooking.Domain;

namespace CpdBooking.UnitTests;

/// <summary>Predictable references: REF-1, REF-2...</summary>
internal sealed class SequentialReferences : IBookingReferenceGenerator
{
    private int _next;

    public string Create() => $"REF-{++_next}";
}


internal static class Courses
{
    public static readonly DateTimeOffset Now = new(2026, 11, 9, 10, 0, 0, TimeSpan.Zero);

    public static Course StartingIn(TimeSpan fromNow, int capacity = 10) =>
        new(Guid.NewGuid(), "Phonics", "Hub", "Summary", Now + fromNow, capacity);

}
