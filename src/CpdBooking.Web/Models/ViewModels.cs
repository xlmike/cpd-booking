using System.ComponentModel.DataAnnotations;
using CpdBooking.Application;
using CpdBooking.Domain;

namespace CpdBooking.Web.Models;

public sealed record CourseListViewModel(string? Search, IReadOnlyList<CourseSummary> Courses);

public sealed class BookingFormViewModel
{
    [Required(ErrorMessage = "Enter your email address")]
    [StringLength(EmailAddress.MaxLength, ErrorMessage = "Email address must be 254 characters or fewer")]
    [EmailAddress(ErrorMessage = "Enter an email address in the correct format, like name@example.com")]
    public string? Email { get; set; }

    public CourseSummary? Course { get; set; }
}
