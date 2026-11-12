using System.ComponentModel.DataAnnotations;
using CpdBooking.Application;
using CpdBooking.Domain;
using CpdBooking.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CpdBooking.Web.Api;

/// <summary>
/// Version 1 of the courses API. Thin controller: HTTP in, HTTP out. Rules live in the domain.
/// No cookies are used here, so it isn't exposed to CSRF; JSON bodies also need a CORS preflight.
/// </summary>
[ApiController]
[Route("api/v1/courses")]
public sealed class CoursesApiController(CourseCatalogue catalogue, BookingService bookings) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CourseSummary>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CourseSummary>>> List([FromQuery, StringLength(CourseCatalogue.MaxSearchLength)] string? search, CancellationToken cancellationToken) =>
        Ok(await catalogue.ListUpcomingAsync(search, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CourseSummary>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseSummary>> Get(Guid id, CancellationToken cancellationToken)
    {
        var course = await catalogue.GetAsync(id, cancellationToken);
        return course is null ? Problem(statusCode: StatusCodes.Status404NotFound, title: "Course not found") : Ok(course);
    }

    [HttpPost("{id:guid}/bookings")]
    [EnableRateLimiting(RateLimitPolicies.Bookings)]
    [ProducesResponseType<BookingCreated>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Book(Guid id, CreateBookingRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await bookings.BookAsync(id, request.Email, cancellationToken);

        return result.Error switch
        {
            BookingError.None => CreatedAtAction(nameof(Get), new { id }, new BookingCreated(result.Booking!.Reference, id)),
            BookingError.CourseNotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Course not found"),
            BookingError.InvalidEmail => ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { [nameof(request.Email)] = ["Enter an email address in the correct format, like name@example.com"] })),
            BookingError.BookingClosed => Problem(statusCode: StatusCodes.Status409Conflict, title: "Bookings for this course have closed"),
            BookingError.CourseFull => Problem(statusCode: StatusCodes.Status409Conflict, title: "This course is full"),
            BookingError.AlreadyBooked => Problem(statusCode: StatusCodes.Status409Conflict, title: "This email address already has a booking on this course"),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError),
        };
    }
}

public sealed record CreateBookingRequest([Required, StringLength(EmailAddress.MaxLength)] string Email);

public sealed record BookingCreated(string Reference, Guid CourseId);
