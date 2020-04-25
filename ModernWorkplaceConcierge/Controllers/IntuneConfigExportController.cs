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
        [HttpPost]
        public async System.Threading.Tasks.Task<FileResult> DownloadAsync(string clientId)
        {
            GraphIntune graphIntune = new GraphIntune(clientId);

            var DeviceCompliancePolicies = await graphIntune.GetDeviceCompliancePoliciesAsync();
            var DeviceConfigurations = await graphIntune.GetDeviceConfigurationsAsync();
            var ManagedAppProtection = await graphIntune.GetManagedAppProtectionAsync();
            var WindowsAutopilotDeploymentProfiles = await graphIntune.GetWindowsAutopilotDeploymentProfiles();
            var DeviceManagementScripts = await graphIntune.GetDeviceManagementScriptsAsync();
            var DeviceEnrollmentConfig = await graphIntune.GetDeviceEnrollmentConfigurationsAsync();
            var ScopeTags = await graphIntune.GetRoleScopeTagsAsync();
            var RoleAssignments = await graphIntune.GetRoleAssignmentsAsync();

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
                        string fileName = FilenameHelper.ProcessFileName(item.DisplayName);
                        var zipArchiveEntry = archive.CreateEntry("DeviceConfiguration\\" + fileName + ".json", CompressionLevel.Fastest);
                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                    }

                    foreach (DeviceCompliancePolicy item in DeviceCompliancePolicies)
                    {
                        byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented));
                        string fileName = FilenameHelper.ProcessFileName(item.DisplayName);
                        var zipArchiveEntry = archive.CreateEntry("DeviceCompliancePolicy\\" + fileName + ".json", CompressionLevel.Fastest);
                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                    }

                    foreach (ManagedAppPolicy item in ManagedAppProtection)
                    {
                        if (item.ODataType.Equals("#microsoft.graph.iosManagedAppProtection") || item.ODataType.Equals("#microsoft.graph.androidManagedAppProtection"))
                        {
                            var assignedApps = await graphIntune.GetManagedAppProtectionAssignmentAsync(item.Id);

                            // Create json object from mam policy
                            JObject appProtectionPolicy = JObject.FromObject(item);
                            JArray appProtectionPolicyAssignedApps = JArray.FromObject(assignedApps);

                            // Add assigned apps to export
                            appProtectionPolicy.Add("assignedApps", appProtectionPolicyAssignedApps);

                            byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(appProtectionPolicy, Formatting.Indented));
                            string fileName = FilenameHelper.ProcessFileName(item.DisplayName);
                            var zipArchiveEntry = archive.CreateEntry("ManagedAppPolicy\\" + fileName + ".json", CompressionLevel.Fastest);
                            using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                        }
                        else if (item.ODataType.Equals("#microsoft.graph.targetedManagedAppConfiguration"))
                        {
                            var assignedApps = await graphIntune.GetTargetedManagedAppConfigurationsAssignedAppsAsync(item.Id);

                            // Create json object from mam policy
                            JObject appProtectionPolicy = JObject.FromObject(item);
                            JArray appProtectionPolicyAssignedApps = JArray.FromObject(assignedApps);

                            // Add assigned apps to export
                            appProtectionPolicy.Add("assignedApps", appProtectionPolicyAssignedApps);

                            byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(appProtectionPolicy, Formatting.Indented));
                            string fileName = FilenameHelper.ProcessFileName(item.DisplayName);
                            var zipArchiveEntry = archive.CreateEntry("ManagedAppPolicy\\" + "ManagedAppConfiguration_" + fileName + ".json", CompressionLevel.Fastest);
                            using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);

                        }
                        else
                        {
                            byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented));
                            string fileName = FilenameHelper.ProcessFileName(item.DisplayName);
                            var zipArchiveEntry = archive.CreateEntry("ManagedAppPolicy\\" + fileName + ".json", CompressionLevel.Fastest);
                            using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                        }
                    }

                    foreach (WindowsAutopilotDeploymentProfile item in WindowsAutopilotDeploymentProfiles)
                    {
                        byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented));
                        string fileName = FilenameHelper.ProcessFileName(item.DisplayName);
                        var zipArchiveEntry = archive.CreateEntry("WindowsAutopilotDeploymentProfile\\" + fileName + ".json", CompressionLevel.Fastest);
                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                    }

                    foreach (DeviceManagementScript item in DeviceManagementScripts)
                    {
                        string fixedItem = await graphIntune.GetDeviceManagementScriptRawAsync(item.Id);
                        byte[] temp = Encoding.UTF8.GetBytes(fixedItem);
                        string fileName = FilenameHelper.ProcessFileName(item.DisplayName);
                        var zipArchiveEntry = archive.CreateEntry("DeviceManagementScript\\" + fileName + ".json", CompressionLevel.Fastest);
                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                    }

                    foreach (RoleScopeTag item in ScopeTags)
                    {
                        byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented));
                        string fileName = FilenameHelper.ProcessFileName(item.DisplayName);
                        var zipArchiveEntry = archive.CreateEntry("RoleScopeTags\\" + fileName + ".json", CompressionLevel.Fastest);
                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                    }

                    foreach (DeviceAndAppManagementRoleAssignment item in RoleAssignments)
                    {
                        byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented));
                        string fileName = FilenameHelper.ProcessFileName(item.DisplayName);
                        var zipArchiveEntry = archive.CreateEntry("RoleAssignments\\" + fileName + ".json", CompressionLevel.Fastest);
                        using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                    }
                }

                string domainName = await GraphHelper.GetDefaultDomain(clientId);

                return File(ms.ToArray(), "application/zip", "IntuneConfig_" + domainName + ".zip");
            }
        }
    }
}