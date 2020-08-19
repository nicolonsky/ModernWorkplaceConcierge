using Microsoft.AspNetCore.Mvc;

namespace ModernWorkplaceConcierge.Controllers
{
    public class AboutController : BaseController
    {
        // GET: About
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }
    }
}