using System;
using System.Text;
using System.Web.Mvc;
using ModernWorkplaceConcierge.Helpers;
using Newtonsoft.Json;

namespace ModernWorkplaceConcierge.Controllers
{
    [Authorize]
    [HandleError]
    public class AutoPilotConfigurationController : BaseController
    {
        // GET: AutoPilotConfigurationJSON
        public async System.Threading.Tasks.Task<ActionResult> Index()
        {
            GraphIntune graphIntune = new GraphIntune(null);
            var AutopilotProfiles = await graphIntune.GetWindowsAutopilotDeploymentProfiles();
            return View(AutopilotProfiles);
        }

        public async System.Threading.Tasks.Task<ActionResult> Detail(String Id)
        {
            GraphIntune graphIntune = new GraphIntune(null);
            var AutopilotProfile = await graphIntune.GetWindowsAutopilotDeploymentProfile(Id);
            return View(AutopilotProfile);
        }

        public async System.Threading.Tasks.Task<FileResult> DownloadAutopilotConfigurationJSON(string Id)
        {
            GraphIntune graphIntune = new GraphIntune(null);
            var profile =  await graphIntune.GetWindowsAutopilotDeploymentProfile(Id);
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