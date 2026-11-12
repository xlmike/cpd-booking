using CpdBooking.Application;
using CpdBooking.Domain;
using CpdBooking.Web.Models;
using CpdBooking.Web.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CpdBooking.Web.Controllers;

/// <summary>Uses Post/Redirect/Get, and rejects any POST without a valid anti-forgery token (CSRF).</summary>
[AutoValidateAntiforgeryToken]
public sealed class BookingsController(CourseCatalogue catalogue, BookingService bookings) : Controller
{
    [HttpGet("/courses/{courseId:guid}/book")]
    public async Task<IActionResult> Create(Guid courseId, CancellationToken cancellationToken)
    {
        var course = await catalogue.GetAsync(courseId, cancellationToken);
        if (course is null)
        {
            return NotFound();
        }

        return View(new BookingFormViewModel { Course = course });
    }

    // Binds only to the view model, never to a domain entity, so extra posted fields are ignored (over-posting).
    [HttpPost("/courses/{courseId:guid}/book")]
    [EnableRateLimiting(RateLimitPolicies.Bookings)]
    public async Task<IActionResult> Create(Guid courseId, [Bind(nameof(BookingFormViewModel.Email))] BookingFormViewModel form, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(form);
        var course = await catalogue.GetAsync(courseId, cancellationToken);
        if (course is null)
        {
            return NotFound();
        }

        form.Course = course;
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var result = await bookings.BookAsync(courseId, form.Email, cancellationToken);
        if (result.IsSuccess)
        {
            return RedirectToAction(nameof(Confirmation), new { reference = result.Booking!.Reference });
        }

        ModelState.AddModelError(nameof(form.Email), MessageFor(result.Error));
        return View(form);
    }

    [HttpGet("/bookings/{reference}/confirmation")]
    public async Task<IActionResult> Confirmation(string reference, CancellationToken cancellationToken)
    {
        var confirmation = await catalogue.GetConfirmationAsync(reference, cancellationToken);
        return confirmation is null ? NotFound() : View(confirmation);
    }

    // Error messages follow GOV.UK Design System wording: say what happened and what to do.
    private static string MessageFor(BookingError error) => error switch
    {
        BookingError.InvalidEmail => "Enter an email address in the correct format, like name@example.com",
        BookingError.AlreadyBooked => "This email address already has a booking on this course",
        BookingError.CourseFull => "This course is now full. Choose another course",
        BookingError.BookingClosed => "Bookings for this course have closed. Choose another course",
        _ => "Sorry, there is a problem with the service. Try again later",
    };
}
