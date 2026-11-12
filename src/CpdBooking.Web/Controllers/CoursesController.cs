using CpdBooking.Application;
using CpdBooking.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CpdBooking.Web.Controllers;

public sealed class CoursesController(CourseCatalogue catalogue) : Controller
{
    [HttpGet("/")]
    [HttpGet("/courses")]
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken)
    {
        var courses = await catalogue.ListUpcomingAsync(search, cancellationToken);
        return View(new CourseListViewModel(search?.Trim(), courses));
    }

    [HttpGet("/courses/{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var course = await catalogue.GetAsync(id, cancellationToken);
        return course is null ? NotFound() : View(course);
    }
}
