using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using IntuneConcierge.Helpers;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace IntuneConcierge.Controllers
{
    [Authorize]
    public class AutoPilotConfigurationJSONController : BaseController
    {
        // GET: AutoPilotConfigurationJSON
        public async System.Threading.Tasks.Task<ActionResult> Index()
        {
            var AutopilotProfiles = await GraphHelper.GetWindowsAutopilotDeploymentProfiles();

            return View(AutopilotProfiles);
        }

        public async System.Threading.Tasks.Task<ActionResult> Detail(String Id)
        {
            var AutopilotProfiles = await GraphHelper.GetWindowsAutopilotDeploymentProfiles(Id);

            return View(AutopilotProfiles);
        }

        public async System.Threading.Tasks.Task<FileResult> DownloadAutopilotConfigurationJSON(string Id)
        {
            var profile =  await GraphHelper.GetWindowsAutopilotDeploymentProfiles(Id);

            var org = await GraphHelper.GetOrgDetailsAsync();

            Helpers.WindowsAutopilotDeploymentProfile windowsAutopilotDeploymentProfile = new Helpers.WindowsAutopilotDeploymentProfile(profile, org);

            byte[] autopilotconfiguraton = System.Text.Encoding.Default.GetBytes(JsonConvert.SerializeObject(windowsAutopilotDeploymentProfile,

                 new JsonSerializerSettings()
                 {
                     NullValueHandling = NullValueHandling.Ignore,
                     Formatting = Formatting.Indented
                 } 
                ).ToString());

            return File(autopilotconfiguraton, "application/json", "AutoPilotConfiguration.json");
        }
    }
}