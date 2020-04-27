using Microsoft.Graph;
using ModernWorkplaceConcierge.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Web.Mvc;

namespace ModernWorkplaceConcierge.Controllers
{
    [Authorize]
    public class IntuneConfigExportController : BaseController
    {
        [HttpPost]
        public async System.Threading.Tasks.Task<FileResult> DownloadAsync(string clientId)
        {
            GraphIntune graphIntune = new GraphIntune(clientId);
            SignalRMessage signalRMessage = new SignalRMessage(clientId);

            try
            {
                var deviceCompliancePolicies = await graphIntune.GetDeviceCompliancePoliciesAsync();
                var deviceConfigurations = await graphIntune.GetDeviceConfigurationsAsync();
                var managedAppProtection = await graphIntune.GetManagedAppProtectionAsync();
                var windowsAutopilotDeploymentProfiles = await graphIntune.GetWindowsAutopilotDeploymentProfiles();
                var deviceManagementScripts = await graphIntune.GetDeviceManagementScriptsAsync();
                var deviceEnrollmentConfig = await graphIntune.GetDeviceEnrollmentConfigurationsAsync();
                var scopeTags = await graphIntune.GetRoleScopeTagsAsync();
                var roleAssignments = await graphIntune.GetRoleAssignmentsAsync();

                //var gpos = await graphIntune.GetGroupPolicyConfigurationsAsync();
                //foreach (GroupPolicyConfiguration gpo in gpos)
                //{
                //    var values = await graphIntune.GetGroupPolicyDefinitionValuesAsync(gpo.Id);
                //    signalRMessage.sendMessage(JsonConvert.SerializeObject(gpo, Formatting.Indented) + JsonConvert.SerializeObject(values, Formatting.Indented));

                //    foreach (GroupPolicyDefinitionValue value in values)
                //    {
                //        var res = await graphIntune.GetGroupPolicyPresentationValuesAsync(value.Id);

                //        signalRMessage.sendMessage(JsonConvert.SerializeObject(res, Formatting.Indented));

                //    }
                //}

                using (MemoryStream ms = new MemoryStream())
                {
                    using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                    {
                        foreach (DeviceEnrollmentConfiguration item in deviceEnrollmentConfig)
                        {
                            byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented));
                            var zipArchiveEntry = archive.CreateEntry("DeviceEnrollmentConfiguration\\" + ".json", CompressionLevel.Fastest);
                            using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                        }

                        foreach (DeviceConfiguration item in deviceConfigurations)
                        {
                            byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented));
                            string fileName = FilenameHelper.ProcessFileName(item.DisplayName);
                            var zipArchiveEntry = archive.CreateEntry("DeviceConfiguration\\" + fileName + "_" + item.Id.Substring(0, 8) + ".json", CompressionLevel.Fastest);
                            using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                        }

                        foreach (DeviceCompliancePolicy item in deviceCompliancePolicies)
                        {
                            byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented));
                            string fileName = FilenameHelper.ProcessFileName(item.DisplayName);
                            var zipArchiveEntry = archive.CreateEntry("DeviceCompliancePolicy\\" + fileName + "_" + item.Id.Substring(0, 8) + ".json", CompressionLevel.Fastest);
                            using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                        }

                        foreach (ManagedAppPolicy item in managedAppProtection)
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
                                var zipArchiveEntry = archive.CreateEntry("ManagedAppPolicy\\" + fileName + "_" + item.Id.Substring(0, 8) + ".json", CompressionLevel.Fastest);
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
                                var zipArchiveEntry = archive.CreateEntry("ManagedAppPolicy\\" + "ManagedAppConfiguration_" + fileName + "_" + item.Id.Substring(0, 8) + ".json", CompressionLevel.Fastest);
                                using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                            }
                            else
                            {
                                byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented));
                                string fileName = FilenameHelper.ProcessFileName(item.DisplayName);
                                var zipArchiveEntry = archive.CreateEntry("ManagedAppPolicy\\" + fileName + "_" + item.Id.Substring(0, 8) + ".json", CompressionLevel.Fastest);
                                using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                            }
                        }

                        foreach (WindowsAutopilotDeploymentProfile item in windowsAutopilotDeploymentProfiles)
                        {
                            byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented));
                            string fileName = FilenameHelper.ProcessFileName(item.DisplayName);
                            var zipArchiveEntry = archive.CreateEntry("WindowsAutopilotDeploymentProfile\\" + fileName + "_" + item.Id.Substring(0, 8) + ".json", CompressionLevel.Fastest);
                            using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                        }

                        foreach (DeviceManagementScript item in deviceManagementScripts)
                        {
                            string fixedItem = await graphIntune.GetDeviceManagementScriptRawAsync(item.Id);
                            byte[] temp = Encoding.UTF8.GetBytes(fixedItem);
                            string fileName = FilenameHelper.ProcessFileName(item.DisplayName);
                            var zipArchiveEntry = archive.CreateEntry("DeviceManagementScript\\" + fileName + "_" + item.Id.Substring(0, 8) + ".json", CompressionLevel.Fastest);
                            using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                        }

                        foreach (RoleScopeTag item in scopeTags)
                        {
                            byte[] temp = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented));
                            string fileName = FilenameHelper.ProcessFileName(item.DisplayName);
                            var zipArchiveEntry = archive.CreateEntry("RoleScopeTags\\" + fileName + ".json", CompressionLevel.Fastest);
                            using (var zipStream = zipArchiveEntry.Open()) zipStream.Write(temp, 0, temp.Length);
                        }

                        foreach (DeviceAndAppManagementRoleAssignment item in roleAssignments)
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
            catch (Exception e)
            {
                signalRMessage.sendMessage($"Error {e.Message}");
                return File(new MemoryStream(), "application/zip", "IntuneConfig_.zip");
            }
        }
    }
}