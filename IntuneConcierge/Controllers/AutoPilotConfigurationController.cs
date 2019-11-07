using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using IntuneConcierge.Helpers;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace IntuneConcierge.Controllers
{
    [Authorize]
    public class AutoPilotConfigurationController : BaseController
    {
        // GET: AutoPilotConfigurationJSON
        public async System.Threading.Tasks.Task<ActionResult> Index()
        {
            var AutopilotProfiles = await GraphHelper.GetWindowsAutopilotDeploymentProfiles();

            return View(AutopilotProfiles);
        }

        public async System.Threading.Tasks.Task<ActionResult> Detail(String Id)
        {
            var AutopilotProfile = await GraphHelper.GetWindowsAutopilotDeploymentProfiles(Id);

            return View(AutopilotProfile);
        }

        public async System.Threading.Tasks.Task<FileResult> DownloadAutopilotConfigurationJSON(string Id)
        {
            var profile =  await GraphHelper.GetWindowsAutopilotDeploymentProfiles(Id);

            var org = await GraphHelper.GetOrgDetailsAsync();

            // Create a new AutopilotConfiguration based on custom model and pass AutopilotProfile and Organizational details from Graph
            AutopilotConfiguration windowsAutopilotDeploymentProfile = new AutopilotConfiguration(profile, org);

            // 1250 is ANSI encoding required for the AutopilotConfiguration.json!
            byte[] autopilotconfiguraton = Encoding.GetEncoding(1250).GetBytes(JsonConvert.SerializeObject(windowsAutopilotDeploymentProfile,
                // remove nullvalues from output and pretty format that JSON
                 new JsonSerializerSettings()
                 {
                     NullValueHandling = NullValueHandling.Ignore,
                     Formatting = Formatting.Indented
                 } 
                ).ToString());

            Response.Clear();
            Response.AddHeader("Content-Disposition", "attachment; filename=" + "AutoPilotConfiguration.json");
            Response.Charset = Encoding.GetEncoding(1250).WebName;
            Response.HeaderEncoding = Encoding.GetEncoding(1250);
            Response.ContentEncoding = Encoding.GetEncoding(1250);
            Response.ContentType = "application/json";
            Response.BinaryWrite(autopilotconfiguraton);
            Response.End();
            return null;

            //return File(autopilotconfiguraton, "application/json", "AutoPilotConfiguration.json");
        }
    }
}