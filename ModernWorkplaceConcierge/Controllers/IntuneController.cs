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


        //https://medium.com/@xavierpenya/how-to-download-zip-files-in-asp-net-core-f31b5c371998
        //https://www.ryadel.com/en/create-zip-file-archive-programmatically-actionresult-asp-net-core-mvc-c-sharp/

        public async System.Threading.Tasks.Task<FileResult> DownloadAsync()
        {
            var DeviceCompliancePolicies = await GraphHelper.GetDeviceCompliancePoliciesAsync();

            var DeviceConfigurations = await GraphHelper.GetDeviceConfigurationsAsync();

            var ManagedAppProtection = await GraphHelper.GetManagedAppProtectionAsync();

            var WindowsAutopilotDeploymentProfiles = await GraphHelper.GetWindowsAutopilotDeploymentProfiles();

            using (MemoryStream ms = new MemoryStream())
            {
                using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                {
                    foreach (Microsoft.Graph.DeviceConfiguration item in DeviceConfigurations)
                    {
                        byte[] temp = System.Text.Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented).ToString());

                        var zipArchiveEntry = archive.CreateEntry("DeviceConfigurations\\"+item.DisplayName+".json", CompressionLevel.Fastest);

                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                    }

                    foreach (Microsoft.Graph.DeviceCompliancePolicy item in DeviceCompliancePolicies)
                    {
                        byte[] temp = System.Text.Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented).ToString());

                        var zipArchiveEntry = archive.CreateEntry("DeviceCompliancePolicies\\" + item.DisplayName + ".json", CompressionLevel.Fastest);

                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                    }

                    foreach (Microsoft.Graph.ManagedAppPolicy item in ManagedAppProtection)
                    {
                        byte[] temp = System.Text.Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented).ToString());

                        var zipArchiveEntry = archive.CreateEntry("ManagedAppPolicy\\" + item.DisplayName + ".json", CompressionLevel.Fastest);

                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                    }

                    foreach (Microsoft.Graph.WindowsAutopilotDeploymentProfile item in WindowsAutopilotDeploymentProfiles)
                    {
                        byte[] temp = System.Text.Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented).ToString());

                        var zipArchiveEntry = archive.CreateEntry("WindowsAutopilotDeploymentProfiles\\" + item.DisplayName + ".json", CompressionLevel.Fastest);

                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                    }
                }
                return File(ms.ToArray(), "application/zip", "Archive.zip");
            }
        }
    }
}