using ModernWorkplaceConcierge.Helpers;
using System.IO;
using System.IO.Compression;
using System.Web.Mvc;
using Newtonsoft.Json;
using Microsoft.Graph;
using System.Text;
using Newtonsoft.Json.Linq;

namespace ModernWorkplaceConcierge.Controllers
{
    [Authorize]
    public class IntuneConfigExportController : BaseController
    {
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
            var ScopeTags = await GraphHelper.GetRoleScopeTags();

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
                        var zipArchiveEntry = archive.CreateEntry("DeviceConfiguration\\" + item.DisplayName + ".json", CompressionLevel.Fastest);
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
                        if (item.ODataType.Equals("#microsoft.graph.iosManagedAppProtection") || item.ODataType.Equals("#microsoft.graph.androidManagedAppProtection"))
                        {
                            var assignedApps = await GraphHelper.GetManagedAppProtectionAssignmentAsync(item.Id);

                            // Create json object from mam policy
                            JObject appProtectionPolicy = JObject.FromObject(item);
                            JArray appProtectionPolicyAssignedApps = JArray.FromObject(assignedApps);

                            // Add assigned apps to export
                            appProtectionPolicy.Add("assignedApps", appProtectionPolicyAssignedApps);

                            byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(appProtectionPolicy, Formatting.Indented));
                            var zipArchiveEntry = archive.CreateEntry("ManagedAppPolicy\\" + item.DisplayName + ".json", CompressionLevel.Fastest);
                            using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                        }
                        else if (item.ODataType.Equals("#microsoft.graph.targetedManagedAppConfiguration"))
                        {
                            var assignedApps = await GraphHelper.GetTargetedManagedAppConfigurationsAssignedAppsAsync(item.Id);

                            // Create json object from mam policy
                            JObject appProtectionPolicy = JObject.FromObject(item);
                            JArray appProtectionPolicyAssignedApps = JArray.FromObject(assignedApps);

                            // Add assigned apps to export
                            appProtectionPolicy.Add("assignedApps", appProtectionPolicyAssignedApps);

                            byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(appProtectionPolicy, Formatting.Indented));
                            var zipArchiveEntry = archive.CreateEntry("ManagedAppPolicy\\" + "ManagedAppConfiguration_" + item.DisplayName + ".json", CompressionLevel.Fastest);
                            using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);

                        }
                        else
                        {
                            byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented));
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

                    foreach (RoleScopeTag item in ScopeTags)
                    {
                        byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented));
                        var zipArchiveEntry = archive.CreateEntry("RoleScopeTags\\" + item.DisplayName + ".json", CompressionLevel.Fastest);
                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                    }
                }

                string domainName = await GraphHelper.GetDefaultDomain();

                return File(ms.ToArray(), "application/zip", "IntuneConfig_" + domainName + ".zip");
            }
        }
    }
}