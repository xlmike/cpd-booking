using Microsoft.AspNetCore.Mvc;

namespace CpdBooking.Web.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public sealed class ErrorController : Controller
{
    [Route("/error/{statusCode:int?}")]
    public IActionResult Index(int? statusCode)
    {
        var code = statusCode ?? StatusCodes.Status500InternalServerError;
        var originalPath = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodeReExecuteFeature>()?.OriginalPath
            ?? HttpContext.Request.Path.Value ?? string.Empty;

        if (originalPath.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(statusCode: code);
        }

        Response.StatusCode = code;
        return code switch
        {
            StatusCodes.Status404NotFound => View("NotFound"),
            StatusCodes.Status429TooManyRequests => View("TooManyRequests"),
            _ => View("Error"),
        };
    }
}
