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

            Microsoft.Graph.WindowsAutopilotDeploymentProfile profile= await GraphHelper.GetWindowsAutopilotDeploymentProfiles(Id);

            Helpers.WindowsAutopilotDeploymentProfile windowsAutopilotDeploymentProfile = new Helpers.WindowsAutopilotDeploymentProfile(profile);

            byte[] autopilotconfiguraton = System.Text.Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(windowsAutopilotDeploymentProfile, Formatting.Indented).ToString());

            return File(autopilotconfiguraton, "application/Json", "AutoPilotConfiguration.json");
        }
    }
}