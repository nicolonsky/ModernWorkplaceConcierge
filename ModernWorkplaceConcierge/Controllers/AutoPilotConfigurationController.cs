using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using ModernWorkplaceConcierge.Helpers;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace ModernWorkplaceConcierge.Controllers
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

            AutopilotConfiguration windowsAutopilotDeploymentProfile = new AutopilotConfiguration(profile, org);

            var enc = Encoding.GetEncoding(1252);

            byte[] autopilotconfiguraton = enc.GetBytes(JsonConvert.SerializeObject(windowsAutopilotDeploymentProfile,
                // remove nullvalues from output and pretty format JSON
                new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                }
               ));

            return File(autopilotconfiguraton, "application/json", "AutoPilotConfigurationFile.json");
        }
    }
}