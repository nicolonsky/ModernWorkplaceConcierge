using IntuneConcierge.Helpers;
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
        public async System.Threading.Tasks.Task<ActionResult> Index()
        {
            var ca = await GraphHelper.GetConditionalAccessPoliciesAsync();

            return View(ca);
        }
    }
}