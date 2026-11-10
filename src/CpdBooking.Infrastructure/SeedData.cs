using CpdBooking.Domain;

namespace CpdBooking.Infrastructure;

/// <summary>Example courses so the app has something to show. Dates are relative to today.</summary>
public static class SeedData
{
    public static IReadOnlyList<Course> Courses(DateTimeOffset now)
    {
        var today = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        return
        [
            new Course(
                Guid.Parse("7d3c1c8e-3b6a-4f53-9a51-1f0f3b1e2a01"),
                "Teaching early reading with phonics",
                "North West Teaching Hub",
                "Practical strategies for systematic synthetic phonics in reception and year 1.",
                today.AddDays(14).AddHours(9),
                capacity: 30),
            new Course(
                Guid.Parse("7d3c1c8e-3b6a-4f53-9a51-1f0f3b1e2a02"),
                "Adaptive teaching for pupils with SEND",
                "Midlands Inclusion Partnership",
                "Plan lessons that work for every pupil, including those with special educational needs and disabilities.",
                today.AddDays(21).AddHours(9),
                capacity: 20),
            new Course(
                Guid.Parse("7d3c1c8e-3b6a-4f53-9a51-1f0f3b1e2a03"),
                "Introduction to computing for primary teachers",
                "Computing Network South",
                "Build confidence teaching algorithms, programming and online safety at key stages 1 and 2.",
                today.AddDays(35).AddHours(13),
                capacity: 2),
            new Course(
                Guid.Parse("7d3c1c8e-3b6a-4f53-9a51-1f0f3b1e2a04"),
                "Behaviour management for early career teachers",
                "Yorkshire Teaching School Alliance",
                "Routines, relationships and responses that build a calm classroom.",
                today.AddDays(1).AddHours(16),
                capacity: 25),
        ];
    }
}
