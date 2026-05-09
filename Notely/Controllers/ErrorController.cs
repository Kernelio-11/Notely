using Microsoft.AspNetCore.Mvc;
using Notely.Models;

namespace Notely.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        public IActionResult Status(int statusCode)
        {
            if (statusCode == 401 || statusCode == 403)
            {
                return View("AccessDenied");
            }

            if (statusCode == 404)
            {
                return View("NotFound");
            }

            var model = new ErrorViewModel { RequestId = HttpContext.TraceIdentifier };
            return View("~/Views/Shared/Error.cshtml", model);
        }

        [Route("Error/AccessDenied")]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [Route("Error/NotFound")]
        public IActionResult NotFoundPage()
        {
            return View("NotFound");
        }
    }
}
