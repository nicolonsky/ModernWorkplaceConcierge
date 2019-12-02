using ModernWorkplaceConcierge.Helpers;
using System;
using System.IO;
using System.IO.Compression;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Microsoft.Graph;
using System.Text;

namespace ModernWorkplaceConcierge.Controllers
{
    [Authorize]
    public class IntuneController : BaseController
    {
        public ActionResult Import()
        {
           
            return View();
        }

        [HttpPost]
        public async System.Threading.Tasks.Task<ActionResult> Upload(HttpPostedFileBase[] files)
        {
            foreach (HttpPostedFileBase file in files)
            {
                try
                {
                    BinaryReader b = new BinaryReader(file.InputStream);
                    byte[] binData = b.ReadBytes(file.ContentLength);

                    string result = Encoding.UTF8.GetString(binData);

                    try {

                        GraphJson json = JsonConvert.DeserializeObject<GraphJson>(result);

                        if (json.OdataValue.Contains("CompliancePolicy"))
                        {

                            //https://github.com/microsoftgraph/powershell-intune-samples/blob/master/CompliancePolicy/CompliancePolicy_Import_FromJSON.ps1

                            DeviceCompliancePolicy deviceCompliancePolicy = JsonConvert.DeserializeObject<DeviceCompliancePolicy>(result);

                            Flash("Not implemented");

                            var response = await GraphHelper.AddDeviceCompliancePolicyAsync(deviceCompliancePolicy);

                            Message("Success", response.ToString());

                        }else if (json.OdataValue.Contains("Configuration"))
                        {
                            DeviceConfiguration deviceConfiguration = JsonConvert.DeserializeObject<DeviceConfiguration>(result);

                            // request fails when true :(
                            deviceConfiguration.SupportsScopeTags = false;

                            var response = await GraphHelper.AddDeviceConfigurationAsync(deviceConfiguration);

                            Message("Success", JsonConvert.SerializeObject(response));

                        }else if (json.OdataValue.Contains("deviceManagementScripts"))
                        {
                            DeviceManagementScript deviceManagementScript = JsonConvert.DeserializeObject<DeviceManagementScript>(result);

                            var response = await GraphHelper.AddDeviceManagementScriptsAsync(deviceManagementScript);

                            Message("Success", response.ToString());

                        }else if (json.OdataValue.Contains("azureADWindowsAutopilotDeploymentProfile"))
                        {
                            WindowsAutopilotDeploymentProfile windowsAutopilotDeploymentProfile = JsonConvert.DeserializeObject<WindowsAutopilotDeploymentProfile>(result);

                            var response = await GraphHelper.AddWindowsAutopilotDeploymentProfile(windowsAutopilotDeploymentProfile);

                            Message("Success", response.ToString());

                        }
                    }
                    catch (Exception e)
                    {
                        Flash(e.Message, e.StackTrace);

                    }

                }
                catch (Exception e)
                {
                    Flash(e.Message);

                }
            }

            return RedirectToAction("Import");
        }

        // GET: Export
        public ActionResult Index()
        {

            return View();
        }

        public async System.Threading.Tasks.Task<ViewResult> DeviceManagementScripts()
        {
            try
            {
                var scripts = await GraphHelper.GetDeviceManagementScriptsAsync();

                return View(scripts);

            }
            catch (Exception e)
            {
                Flash("Error getting DeviceManagementScripts" + e.Message.ToString());

                return View();
            }
        }

        public async System.Threading.Tasks.Task<FileResult> DownloadDeviceManagementScript(String Id)
        {
            DeviceManagementScript script = await GraphHelper.GetDeviceManagementScriptsAsync(Id);

            return File(script.ScriptContent, "text/plain", script.FileName);

        }

        //https://medium.com/@xavierpenya/how-to-download-zip-files-in-asp-net-core-f31b5c371998
        //https://www.ryadel.com/en/create-zip-file-archive-programmatically-actionresult-asp-net-core-mvc-c-sharp/

        public async System.Threading.Tasks.Task<FileResult> DownloadAsync()
        {
            var DeviceCompliancePolicies = await GraphHelper.GetDeviceCompliancePoliciesAsync();
            var DeviceConfigurations = await GraphHelper.GetDeviceConfigurationsAsync();
            var ManagedAppProtection = await GraphHelper.GetManagedAppProtectionAsync();
            var WindowsAutopilotDeploymentProfiles = await GraphHelper.GetWindowsAutopilotDeploymentProfiles();
            var DeviceManagementScripts = await GraphHelper.GetDeviceManagementScriptsAsync();
            var DeviceEnrollmentConfig = await GraphHelper.GetDeviceEnrollmentConfigurationsAsync();

            using (MemoryStream ms = new MemoryStream())
            {
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {

                    foreach (DeviceEnrollmentConfiguration item in DeviceEnrollmentConfig)
                    {
                        byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented));
                        var zipArchiveEntry = archive.CreateEntry("DeviceEnrollmentConfiguration\\" + item.Id + ".json", CompressionLevel.Fastest);
                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                    }
                    
                    foreach (DeviceConfiguration item in DeviceConfigurations)
                    {
                        byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented));
                        var zipArchiveEntry = archive.CreateEntry("DeviceConfiguration\\" + item.DisplayName+".json", CompressionLevel.Fastest);
                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                    }

                    foreach (DeviceCompliancePolicy item in DeviceCompliancePolicies)
                    {
                        byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented));
                        var zipArchiveEntry = archive.CreateEntry("DeviceCompliancePolicy\\" + item.DisplayName + ".json", CompressionLevel.Fastest);
                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                    }

                    foreach (ManagedAppPolicy item in ManagedAppProtection)
                    {

                        byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented));
                        var zipArchiveEntry = archive.CreateEntry("ManagedAppPolicy\\" + item.DisplayName + ".json", CompressionLevel.Fastest);
                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);

                        //get assigned apps to policy
                        /*
                        string assignedApps = await GraphHelper.GetManagedAppProtectionAssignmentAsync(item.Id);
                        temp = Encoding.UTF8.GetBytes(assignedApps);
                        zipArchiveEntry = archive.CreateEntry("ManagedAppPolicy\\" + item.DisplayName + "_assignedApps" + ".json", CompressionLevel.Fastest);
                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                        */
                    }

                    foreach (WindowsAutopilotDeploymentProfile item in WindowsAutopilotDeploymentProfiles)
                    {
                        byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented));
                        var zipArchiveEntry = archive.CreateEntry("WindowsAutopilotDeploymentProfile\\" + item.DisplayName + ".json", CompressionLevel.Fastest);
                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                    }

                    foreach (DeviceManagementScript item in DeviceManagementScripts)
                    {
                        string fixedItem = await GraphHelper.GetDeviceManagementScriptRawAsync(item.Id);
                        byte[] temp = Encoding.UTF8.GetBytes(fixedItem);
                        var zipArchiveEntry = archive.CreateEntry("DeviceManagementScript\\" + item.DisplayName + ".json", CompressionLevel.Fastest);
                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                    }
                }

                string domainName = await GraphHelper.GetDefaultDomain();

                return File(ms.ToArray(), "application/zip", "IntuneConfig_" + domainName + ".zip");
            }
        }
    }
}