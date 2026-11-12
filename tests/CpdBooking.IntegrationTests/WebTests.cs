using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CpdBooking.IntegrationTests;

public partial class WebTests(CpdBookingWebFactory factory) : IClassFixture<CpdBookingWebFactory>
{
    private const string BookingForm = "/courses/7d3c1c8e-3b6a-4f53-9a51-1f0f3b1e2a02/book";

    [Fact]
    public async Task Pages_send_security_headers()
    {
        using var response = await factory.CreateClient().GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var csp = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
        Assert.Contains("script-src 'self'", csp, StringComparison.Ordinal);
        Assert.Contains("frame-ancestors 'none'", csp, StringComparison.Ordinal);
        Assert.Equal("nosniff", Assert.Single(response.Headers.GetValues("X-Content-Type-Options")));
        Assert.False(response.Headers.Contains("Server"));
    }

    [Fact]
    public async Task Posting_the_booking_form_without_an_anti_forgery_token_is_rejected()
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["Email"] = "a@school.org.uk" });

        using var response = await factory.CreateClient().PostAsync(BookingForm, content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Booking_through_the_form_redirects_to_the_confirmation_page()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var form = await client.GetStringAsync(BookingForm);
        var token = AntiForgeryToken().Match(form).Groups[1].Value;

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "form-test@school.org.uk",
            ["__RequestVerificationToken"] = token,
        });
        using var response = await client.PostAsync(BookingForm, content);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Matches("^/bookings/CPD-[A-Z0-9]{4}-[A-Z0-9]{4}/confirmation$", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task An_empty_form_shows_the_gov_uk_error_summary()
    {
        var client = factory.CreateClient();
        var form = await client.GetStringAsync(BookingForm);
        var token = AntiForgeryToken().Match(form).Groups[1].Value;

        using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token });
        using var response = await client.PostAsync(BookingForm, content);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<title>Error: What is your email address?", html, StringComparison.Ordinal);
        Assert.Contains("govuk-error-summary", html, StringComparison.Ordinal);
        Assert.Contains("href=\"#Email\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_health_check_reports_healthy()
    {
        var body = await factory.CreateClient().GetStringAsync("/health");

        Assert.Equal("Healthy", body);
    }

    [Fact]
    public async Task An_unknown_page_returns_the_404_page()
    {
        using var response = await factory.CreateClient().GetAsync("/no-such-page");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Page not found", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex AntiForgeryToken();
}
