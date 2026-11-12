using Microsoft.AspNetCore.Mvc;

namespace CpdBooking.Web.Controllers;

public sealed class InfoController : Controller
{
    [HttpGet("/accessibility")]
    public IActionResult Accessibility() => View();
}
