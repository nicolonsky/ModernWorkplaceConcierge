using ModernWorkplaceConcierge.Helpers;
using System.Web.Mvc;

namespace ModernWorkplaceConcierge.Controllers
{
    [CustomAuthorization(Roles = "AdvancedUser")]
    public class AdvancedController : BaseController
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}