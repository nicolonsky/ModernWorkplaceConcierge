using ModernWorkplaceConcierge.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
        // GET: Export
        public ActionResult Index()
        {

            return View();
        }

        public async System.Threading.Tasks.Task<ViewResult> DeviceManagementScripts()
        {
            var scripts = await GraphHelper.GetDeviceManagementScriptsAsync();

            return View(scripts);
        }

        public async System.Threading.Tasks.Task<FileResult> DownloadDeviceManagementScript(String Id)
        {
            DeviceManagementScript script = await GraphHelper.GetDeviceManagementScriptsAsync(Id);

            byte[] data = System.Convert.FromBase64String(script.ScriptContent.ToString());

            String base64Decoded = Encoding.GetEncoding(1250).GetString(data);

            byte[] res = Encoding.GetEncoding(1250).GetBytes(base64Decoded);

            return File(res, "text/plain", script.FileName);
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

            using (MemoryStream ms = new MemoryStream())
            {
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    foreach (DeviceConfiguration item in DeviceConfigurations)
                    {
                        byte[] temp = System.Text.Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented).ToString());

                        var zipArchiveEntry = archive.CreateEntry("DeviceConfigurations\\"+item.DisplayName+".json", CompressionLevel.Fastest);

                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                    }

                    foreach (DeviceCompliancePolicy item in DeviceCompliancePolicies)
                    {
                        byte[] temp = System.Text.Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented).ToString());

                        var zipArchiveEntry = archive.CreateEntry("DeviceCompliancePolicies\\" + item.DisplayName + ".json", CompressionLevel.Fastest);

                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                    }

                    foreach (ManagedAppPolicy item in ManagedAppProtection)
                    {
                        byte[] temp = System.Text.Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented).ToString());

                        var zipArchiveEntry = archive.CreateEntry("ManagedAppPolicy\\" + item.DisplayName + ".json", CompressionLevel.Fastest);

                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                    }

                    foreach (WindowsAutopilotDeploymentProfile item in WindowsAutopilotDeploymentProfiles)
                    {
                        byte[] temp = System.Text.Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented).ToString());

                        var zipArchiveEntry = archive.CreateEntry("WindowsAutopilotDeploymentProfiles\\" + item.DisplayName + ".json", CompressionLevel.Fastest);

                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                    }

                    foreach (DeviceManagementScript item in DeviceManagementScripts)
                    {
                        byte[] temp = System.Text.Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented).ToString());

                        var zipArchiveEntry = archive.CreateEntry("DeviceManagementScripts\\" + item.DisplayName + ".json", CompressionLevel.Fastest);

                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                    }
                }
                return File(ms.ToArray(), "application/zip", "Archive.zip");
            }
        }
    }
}