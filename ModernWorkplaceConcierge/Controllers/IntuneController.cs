using ModernWorkplaceConcierge.Helpers;
using System;
using System.IO;
using System.IO.Compression;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Microsoft.Graph;
using System.Text;
using System.Linq;
using Newtonsoft.Json.Linq;

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
            try
            {
                if (files.Length > 0 && files[0].FileName.Contains(".json"))
                {
                    foreach (HttpPostedFileBase file in files)
                    {

                        try
                        {
                            BinaryReader b = new BinaryReader(file.InputStream);
                            byte[] binData = b.ReadBytes(file.ContentLength);
                            string result = Encoding.UTF8.GetString(binData);

                            string response = await GraphHelper.AddIntuneConfig(result);

                            Message("Success", response);
                        }
                        catch (Exception e)
                        {
                            Flash(e.Message);
                        }
                    }
                }
                else if (files.Length > 0 && files[0].FileName.Contains(".zip"))
                {
                    try
                    {
                        MemoryStream target = new MemoryStream();
                        files[0].InputStream.CopyTo(target);
                        byte[] data = target.ToArray();

                        using (var zippedStream = new MemoryStream(data))
                        {
                            using (var archive = new ZipArchive(zippedStream))
                            {
                                foreach (var entry in archive.Entries)
                                {
                                    try
                                    {
                                        if (entry != null)
                                        {
                                            if (entry.FullName.Contains("WindowsAutopilotDeploymentProfile") || entry.FullName.Contains("DeviceConfiguration") || entry.FullName.Contains("DeviceCompliancePolicy") || entry.FullName.Contains("DeviceManagementScript") || entry.FullName.Contains("ManagedAppPolicy"))
                                            {
                                                using (var unzippedEntryStream = entry.Open())
                                                {
                                                    using (var ms = new MemoryStream())
                                                    {
                                                        unzippedEntryStream.CopyTo(ms);
                                                        var unzippedArray = ms.ToArray();
                                                        string result = Encoding.UTF8.GetString(unzippedArray);

                                                        if (!string.IsNullOrEmpty(result))
                                                        {
                                                            string response = await GraphHelper.AddIntuneConfig(result);

                                                            if (!(string.IsNullOrEmpty(response)))
                                                            {
                                                                Message("Success", response);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Flash(e.ToString());
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Flash(e.Message);
                    }
                }
                else if (files.Length > 0)
                {
                    Flash("Unsupported file type", files[0].FileName);
                }
            }
            catch (NullReferenceException)
            {
                Flash("Please select a file!");
            }
            catch (Exception e) {
                Flash(e.Message);
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

        public async System.Threading.Tasks.Task<PartialViewResult>PowerShellScriptContent(string Id)
        {
            try
            {
                var scripts = await GraphHelper.GetDeviceManagementScriptsAsync(Id);

                string powerShellCode = Encoding.UTF8.GetString(scripts.ScriptContent);

                return PartialView("_PowerShellScriptContent", powerShellCode);

            }
            catch (Exception e)
            {
                Flash("Error getting DeviceManagementScripts" + e.Message.ToString());

                return PartialView();
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
                        if (item.ODataType.Equals("#microsoft.graph.iosManagedAppProtection"))
                        {
                            string assignedApps = await GraphHelper.GetManagedAppProtectionAssignmentAsync(item.Id, "iosManagedAppProtections");

                            // Create json object from mam policy
                            JObject appProtectionPolicy = JObject.FromObject(item);

                            JObject appPortectionPolicyAssignedApps = JObject.Parse(assignedApps);

                            // Add assigned apps to export
                            appProtectionPolicy.Add("assignedApps", appPortectionPolicyAssignedApps.SelectToken("value"));

                            byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(appProtectionPolicy, Formatting.Indented));
                            var zipArchiveEntry = archive.CreateEntry("ManagedAppPolicy\\" + item.DisplayName + ".json", CompressionLevel.Fastest);
                            using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                        }
                        else if (item.ODataType.Equals("#microsoft.graph.androidManagedAppProtection"))
                        {
                            string assignedApps = await GraphHelper.GetManagedAppProtectionAssignmentAsync(item.Id, "androidManagedAppProtections");

                            // Create json object from mam policy
                            JObject appProtectionPolicy = JObject.FromObject(item);

                            JObject appPortectionPolicyAssignedApps = JObject.Parse(assignedApps);

                            // Add assigned apps to export
                            appProtectionPolicy.Add("assignedApps", appPortectionPolicyAssignedApps.SelectToken("value"));

                            byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(appProtectionPolicy, Formatting.Indented));
                            var zipArchiveEntry = archive.CreateEntry("ManagedAppPolicy\\" + item.DisplayName + ".json", CompressionLevel.Fastest);
                            using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);


                        }
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