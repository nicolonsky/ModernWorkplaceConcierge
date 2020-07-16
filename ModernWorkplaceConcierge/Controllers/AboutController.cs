using System.Web.Mvc;

namespace ModernWorkplaceConcierge.Controllers
{
    public class AboutController : BaseController
    {
        // GET: About
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Terms()
        {
            return View();
        }
    }
}