using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace IntuneConcierge.Controllers
{
    public class ConditionalAccessController : Controller
    {

        /*
         CA policies: https://docs.microsoft.com/en-us/graph/api/conditionalaccessroot-list-policies?view=graph-rest-beta&tabs=http
             */

        // GET: ConditionalAccess
        public ActionResult Index()
        {
            return View();
        }
    }
}