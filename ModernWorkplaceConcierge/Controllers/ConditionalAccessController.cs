using ModernWorkplaceConcierge.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.IO;


namespace ModernWorkplaceConcierge.Controllers
{
    [Authorize]
    public class ConditionalAccessController : BaseController
    {
        [HttpPost]
        public async System.Threading.Tasks.Task<ActionResult> UploadAsync(HttpPostedFileBase file)
        {

            if (file.FileName.Contains(".json"))
            {
                BinaryReader b = new BinaryReader(file.InputStream);
                byte[] binData = b.ReadBytes(file.ContentLength);

                string result = System.Text.Encoding.UTF8.GetString(binData);

                bool res = await GraphHelper.AddConditionalAccessPolicyAsync(result);

            }
           
             return RedirectToAction("Import");
        }

        public ViewResult Import()
        {

            return View();

        }

        /*
            CA policies: https://docs.microsoft.com/en-us/graph/api/conditionalaccessroot-list-policies?view=graph-rest-beta&tabs=http
                */

        // GET: ConditionalAccess
        public async System.Threading.Tasks.Task<ViewResult> Index()
        {
            var ca = await GraphHelper.GetConditionalAccessPoliciesAsync();

            ConditionalAccessPolicies policies = JsonConvert.DeserializeObject<ConditionalAccessPolicies>(ca);
                       
            return View(policies.Value);

        }

        public async System.Threading.Tasks.Task<FileResult> Download(String Id)
        {
            string ca = await GraphHelper.GetConditionalAccessPolicyAsync(Id);

            ConditionalAccessPolicy conditionalAccessPolicy = JsonConvert.DeserializeObject<ConditionalAccessPolicy>(ca);

            byte[] capolicy = Encoding.GetEncoding(1250).GetBytes(JsonConvert.SerializeObject(conditionalAccessPolicy, Formatting.Indented).ToString());

          
            return File(capolicy, "application/json", "CA-Policy" + Id + ".json");
        }
    }
}