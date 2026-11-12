using System.Net;
using System.Net.Http.Json;
using CpdBooking.Application;

namespace CpdBooking.IntegrationTests;

public class ApiTests(CpdBookingWebFactory factory) : IClassFixture<CpdBookingWebFactory>
{
    private const string Phonics = "/api/v1/courses/7d3c1c8e-3b6a-4f53-9a51-1f0f3b1e2a01";
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task The_courses_endpoint_lists_upcoming_courses()
    {
        var courses = await _client.GetFromJsonAsync<List<CourseSummary>>("/api/v1/courses");

        Assert.NotNull(courses);
        Assert.Equal(4, courses.Count);
    }

    [Fact]
    public async Task Booking_returns_201_and_a_second_booking_with_the_same_email_returns_409()
    {
        using var first = await _client.PostAsJsonAsync($"{Phonics}/bookings", new { email = "api-test@school.org.uk" });
        using var second = await _client.PostAsJsonAsync($"{Phonics}/bookings", new { email = "API-TEST@school.org.uk" });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("application/problem+json", second.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task An_invalid_email_returns_a_validation_problem()
    {
        using var response = await _client.PostAsJsonAsync($"{Phonics}/bookings", new { email = "not-an-email" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"Email\"", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_unknown_course_returns_404_problem_details()
    {
        using var response = await _client.GetAsync("/api/v1/courses/00000000-0000-0000-0000-000000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task The_api_never_returns_booking_emails()
    {
        using var booking = await _client.PostAsJsonAsync($"{Phonics}/bookings", new { email = "private@school.org.uk" });
        var body = await _client.GetStringAsync(Phonics);

        Assert.DoesNotContain("private@school.org.uk", body, StringComparison.OrdinalIgnoreCase);
    }
}
