using IntuneConcierge.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public async System.Threading.Tasks.Task<FileResult> Index()
        {
            var ca = await GraphHelper.GetConditionalAccessPoliciesAsync();

            byte[] autopilotconfiguraton = Encoding.GetEncoding(1250).GetBytes(ca);

          return File(autopilotconfiguraton, "application/json", "CA.json");

        }
    }
}