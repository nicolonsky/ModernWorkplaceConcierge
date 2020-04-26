using Microsoft.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ModernWorkplaceConcierge.Helpers
{
    public class GraphIntune : GraphClient
    {
        private string clientId;
        private GraphServiceClient graphServiceClient;
        private SignalRMessage signalRMessage;

        public GraphIntune(string clientId)
        {
            this.clientId = clientId;
            this.signalRMessage = new SignalRMessage(clientId);
            this.graphServiceClient = GetAuthenticatedClient();
        }

        public static string ConvertToApppProtectionAssignment(string AppProtectionPolicy)
        {
            // Get assigned apps
            JObject config = JObject.Parse(AppProtectionPolicy);
            ArrayList assignedApps = new ArrayList();

            foreach (var app in config.SelectToken("assignedApps").Children())
            {
                assignedApps.Add(app.ToObject<ManagedMobileApp>());
            }

            string requestBody = JsonConvert.SerializeObject(assignedApps, Formatting.Indented);
            requestBody = requestBody.Insert(0, "{ \"apps\":");
            requestBody = requestBody.Insert(requestBody.Length, "}");

            return requestBody;
        }

        public async Task<AndroidManagedAppProtection> AddAndroidManagedAppProtectionAsync(AndroidManagedAppProtection managedAppProtection)
        {
            var resource = graphServiceClient.DeviceAppManagement.AndroidManagedAppProtections.Request();
            signalRMessage.sendMessage($"POST: {resource.RequestUrl}");
            var response = await resource.AddAsync(managedAppProtection);
            return response;
        }

        public async Task<AndroidManagedAppProtection> PatchAndroidManagedAppProtectionAsync(AndroidManagedAppProtection managedAppProtection)
        {
            var resource = graphServiceClient.DeviceAppManagement.AndroidManagedAppProtections[managedAppProtection.Id].Request();
            signalRMessage.sendMessage($"PATCH: {resource.RequestUrl}");
            var response = await resource.UpdateAsync(managedAppProtection);
            return response;
        }

        public async Task<DeviceCompliancePolicy> AddDeviceCompliancePolicyAsync(DeviceCompliancePolicy deviceCompliancePolicy)
        {
            var resource = graphServiceClient.DeviceManagement.DeviceCompliancePolicies.Request();
            signalRMessage.sendMessage($"POST: {resource.RequestUrl}");
            var result = await resource.AddAsync(deviceCompliancePolicy);
            return result;
        }

        public async Task<DeviceCompliancePolicy> PatchDeviceCompliancePolicyAsync(DeviceCompliancePolicy deviceCompliancePolicy)
        {
            var resource = graphServiceClient.DeviceManagement.DeviceCompliancePolicies[deviceCompliancePolicy.Id].Request();
            signalRMessage.sendMessage($"PATCH: {resource.RequestUrl}");
            var result = await resource.UpdateAsync(deviceCompliancePolicy);
            return result;
        }

        public async Task<DeviceConfiguration> AddDeviceConfigurationAsync(DeviceConfiguration deviceConfiguration)
        {
            var resource = graphServiceClient.DeviceManagement.DeviceConfigurations.Request();
            signalRMessage.sendMessage($"POST: {resource.RequestUrl}");
            var result = await resource.AddAsync(deviceConfiguration);
            return result;
        }

        public async Task<DeviceConfiguration> PatchDeviceConfigurationAsync(DeviceConfiguration deviceConfiguration)
        {
            deviceConfiguration.SupportsScopeTags = null;
            deviceConfiguration.RoleScopeTagIds = null;

            if (!deviceConfiguration.ODataType.Equals("#microsoft.graph.windowsUpdateForBusinessConfiguration"))
            {
                string requestUrl = $"{graphEndpoint}/deviceManagement/deviceConfigurations/{deviceConfiguration.Id}";
                HttpRequestMessage hrm = new HttpRequestMessage(new HttpMethod("PATCH"), requestUrl)
                {
                    Content = new StringContent(JsonConvert.SerializeObject(deviceConfiguration, new JsonSerializerSettings()
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                    }), Encoding.UTF8, "application/json")
                };

                // Authenticate (add access token) our HttpRequestMessage
                await this.graphServiceClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);
                signalRMessage.sendMessage($"{hrm.Method}: {requestUrl}");

                // Send the request and get the response.
                HttpResponseMessage response = await graphServiceClient.HttpProvider.SendAsync(hrm);
            }
            else
            {
                await AddDeviceConfigurationAsync(deviceConfiguration);
            }

            return deviceConfiguration;
        }

        public async Task<DeviceManagementScript> AddDeviceManagementScriptsAsync(DeviceManagementScript deviceManagementScript)
        {
            var resource = graphServiceClient.DeviceManagement.DeviceManagementScripts.Request();
            signalRMessage.sendMessage($"POST: {resource.RequestUrl}");
            var response = await resource.AddAsync(deviceManagementScript);
            return response;
        }

        public async Task<DeviceManagementScript> PatchDeviceManagementScriptsAsync(DeviceManagementScript deviceManagementScript)
        {
            var resource = graphServiceClient.DeviceManagement.DeviceManagementScripts[deviceManagementScript.Id].Request();
            signalRMessage.sendMessage($"PATCH: {resource.RequestUrl}");
            var response = await resource.UpdateAsync(deviceManagementScript);
            return response;
        }

        public async Task<IosManagedAppProtection> AddIosManagedAppProtectionAsync(IosManagedAppProtection managedAppProtection)
        {
            var resource = graphServiceClient.DeviceAppManagement.IosManagedAppProtections.Request();
            signalRMessage.sendMessage($"POST: {resource.RequestUrl}");
            var response = await resource.AddAsync(managedAppProtection);
            return response;
        }

        public async Task<IosManagedAppProtection> PatchIosManagedAppProtectionAsync(IosManagedAppProtection managedAppProtection)
        {
            var resource = graphServiceClient.DeviceAppManagement.IosManagedAppProtections[managedAppProtection.Id].Request();
            signalRMessage.sendMessage($"PATCH: {resource.RequestUrl}");
            var response = await resource.UpdateAsync(managedAppProtection);
            return response;
        }

        public async Task<TargetedManagedAppConfiguration> AddManagedAppConfigurationAsync(TargetedManagedAppConfiguration managedAppConfiguration)
        {
            var resource = graphServiceClient.DeviceAppManagement.TargetedManagedAppConfigurations.Request();
            signalRMessage.sendMessage($"POST: {resource.RequestUrl}");
            var response = await resource.AddAsync(managedAppConfiguration);
            return response;
        }

        public async Task<TargetedManagedAppConfiguration> PatchManagedAppConfigurationAsync(TargetedManagedAppConfiguration managedAppConfiguration)
        {
            var resource = graphServiceClient.DeviceAppManagement.TargetedManagedAppConfigurations[managedAppConfiguration.Id].Request();
            signalRMessage.sendMessage($"PATCH: {resource.RequestUrl}");
            var response = await resource.UpdateAsync(managedAppConfiguration);
            return response;
        }

        public async Task<WindowsAutopilotDeploymentProfile> AddWindowsAutopilotDeploymentProfile(WindowsAutopilotDeploymentProfile autopilotDeploymentProfile)
        {
            var resource = graphServiceClient.DeviceManagement.WindowsAutopilotDeploymentProfiles.Request();
            signalRMessage.sendMessage($"POST: {resource.RequestUrl}");
            var response = await resource.AddAsync(autopilotDeploymentProfile);
            return response;
        }

        public async Task<WindowsAutopilotDeploymentProfile> PatchWindowsAutopilotDeploymentProfile(WindowsAutopilotDeploymentProfile autopilotDeploymentProfile)
        {
            var resource = graphServiceClient.DeviceManagement.WindowsAutopilotDeploymentProfiles[autopilotDeploymentProfile.Id].Request();
            signalRMessage.sendMessage($"PATCH: {resource.RequestUrl}");
            var response = await resource.UpdateAsync(autopilotDeploymentProfile);
            return response;
        }

        public async Task<IEnumerable<DeviceCompliancePolicy>> GetDeviceCompliancePoliciesAsync()
        {
            var resource = graphServiceClient.DeviceManagement.DeviceCompliancePolicies.Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            var deviceCompliancePolicies = await resource.GetAsync();
            return deviceCompliancePolicies.CurrentPage;
        }

        public async Task<IEnumerable<DeviceConfiguration>> GetDeviceConfigurationsAsync()
        {
            var resource = graphServiceClient.DeviceManagement.DeviceConfigurations.Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            var deviceConfigurations = await resource.GetAsync();
            return deviceConfigurations.CurrentPage;
        }

        // Get's ESP, Enrollment restrictions, WHFB settings etc...
        public async Task<IEnumerable<DeviceEnrollmentConfiguration>> GetDeviceEnrollmentConfigurationsAsync()
        {
            var resource = graphServiceClient.DeviceManagement.DeviceEnrollmentConfigurations.Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            var deviceManagementScripts = await resource.GetAsync();
            return deviceManagementScripts.CurrentPage;
        }

        public async Task<DeviceManagementScript> GetDeviceManagementScriptAsync(string Id)
        {
            var resource = graphServiceClient.DeviceManagement.DeviceManagementScripts[Id].Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            DeviceManagementScript deviceManagementScript = await resource.GetAsync();
            return deviceManagementScript;
        }

        public async Task<string> GetDeviceManagementScriptRawAsync(string Id)
        {
            string requestUrl = $"{graphEndpoint}/deviceManagement/deviceManagementScripts/{Id}";

            HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            await graphServiceClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

            signalRMessage.sendMessage($"{hrm.Method}: {requestUrl}");

            // Send the request and get the response.
            HttpResponseMessage response = await graphServiceClient.HttpProvider.SendAsync(hrm);
            string result = await response.Content.ReadAsStringAsync();
            return result;
        }

        public async Task<IEnumerable<DeviceManagementScript>> GetDeviceManagementScriptsAsync()
        {
            var resource = graphServiceClient.DeviceManagement.DeviceManagementScripts.Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            var result = await resource.GetAsync();
            return result.CurrentPage;
        }

        public async Task<IEnumerable<ManagedMobileApp>> GetManagedAppProtectionAssignmentAsync(string Id)
        {
            var resource = graphServiceClient.DeviceAppManagement.DefaultManagedAppProtections[Id].Apps.Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            var response = await resource.GetAsync();
            return response.CurrentPage;
        }

        public async Task<IEnumerable<ManagedAppPolicy>> GetManagedAppProtectionAsync()
        {
            var resource = graphServiceClient.DeviceAppManagement.ManagedAppPolicies.Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            var managedAppProtection = await resource.GetAsync();
            return managedAppProtection.CurrentPage;
        }

        public async Task<ManagedAppPolicy> GetManagedAppProtectionAsync(string Id)
        {
            var resource = graphServiceClient.DeviceAppManagement.IosManagedAppProtections[Id].Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            var managedAppProtection = await resource.GetAsync();
            return managedAppProtection;
        }

        public async Task<IEnumerable<DeviceAndAppManagementRoleAssignment>> GetRoleAssignmentsAsync()
        {
            var resource = graphServiceClient.DeviceManagement.RoleAssignments.Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            var response = await resource.GetAsync();
            return response;
        }

        public async Task<IEnumerable<RoleScopeTag>> GetRoleScopeTagsAsync()
        {
            var resource = graphServiceClient.DeviceManagement.RoleScopeTags.Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            var response = await resource.GetAsync();
            return response;
        }

        public async Task<IEnumerable<ManagedMobileApp>> GetTargetedManagedAppConfigurationsAssignedAppsAsync(string Id)
        {
            var resource = graphServiceClient.DeviceAppManagement.TargetedManagedAppConfigurations[Id].Apps.Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            var apps = await resource.GetAsync();
            return apps.CurrentPage;
        }

        public async Task<WindowsAutopilotDeploymentProfile> GetWindowsAutopilotDeploymentProfile(string Id)
        {
            var resource = graphServiceClient.DeviceManagement.WindowsAutopilotDeploymentProfiles[Id].Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            WindowsAutopilotDeploymentProfile windowsAutopilotDeploymentProfile = await resource.GetAsync();
            return windowsAutopilotDeploymentProfile;
        }

        public async Task<IEnumerable<WindowsAutopilotDeploymentProfile>> GetWindowsAutopilotDeploymentProfiles()
        {
            var resource = graphServiceClient.DeviceManagement.WindowsAutopilotDeploymentProfiles.Request();

            var windowsAutopilotDeploymentProfiles = await resource.GetAsync();
            return windowsAutopilotDeploymentProfiles.CurrentPage;
        }

        public async Task<AndroidManagedAppProtection> ImportAndroidManagedAppProtectionAsync(string androidManagedAppProtection)
        {
            AndroidManagedAppProtection managedAppProtection = JsonConvert.DeserializeObject<AndroidManagedAppProtection>(androidManagedAppProtection);
            var response = await AddAndroidManagedAppProtectionAsync(managedAppProtection);
            string requestUrl = graphEndpoint + "/deviceAppManagement/androidManagedAppProtections/" + response.Id + "/targetApps";

            // Restore assignment of app protection policy
            string requestBody = ConvertToApppProtectionAssignment(androidManagedAppProtection);
            HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };

            // Authenticate (add access token) our HttpRequestMessage
            await graphServiceClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);
            signalRMessage.sendMessage($"{hrm.Method}: {requestUrl}");
            // Send the request and get the response.
            await graphServiceClient.HttpProvider.SendAsync(hrm);
            return response;
        }

        public async Task<AndroidManagedAppProtection> ImportPatchAndroidManagedAppProtectionAsync(string androidManagedAppProtection)
        {
            AndroidManagedAppProtection managedAppProtection = JsonConvert.DeserializeObject<AndroidManagedAppProtection>(androidManagedAppProtection);
            var response = await PatchAndroidManagedAppProtectionAsync(managedAppProtection);
            string requestUrl = graphEndpoint + "/deviceAppManagement/androidManagedAppProtections/" + response.Id + "/targetApps";

            // Restore assignment of app protection policy
            string requestBody = ConvertToApppProtectionAssignment(androidManagedAppProtection);
            HttpRequestMessage hrm = new HttpRequestMessage(new HttpMethod("PATCH"), requestUrl)
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };

            // Authenticate (add access token) our HttpRequestMessage
            await graphServiceClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);
            signalRMessage.sendMessage($"{hrm.Method}: {requestUrl}");
            // Send the request and get the response.
            await graphServiceClient.HttpProvider.SendAsync(hrm);
            return response;
        }

        public async Task<IosManagedAppProtection> ImportIosManagedAppProtectionAsync(string iosManagedAppProtection)
        {
            IosManagedAppProtection managedAppProtection = JsonConvert.DeserializeObject<IosManagedAppProtection>(iosManagedAppProtection);
            var response = await AddIosManagedAppProtectionAsync(managedAppProtection);
            string requestUrl = graphEndpoint + "/deviceAppManagement/iosManagedAppProtections/" + response.Id + "/targetApps";

            // Restore assignment of app protection policy
            string requestBody = ConvertToApppProtectionAssignment(iosManagedAppProtection);
            HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };

            // Authenticate (add access token) our HttpRequestMessage
            await graphServiceClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);
            signalRMessage.sendMessage($"{hrm.Method}: {requestUrl}");
            // Send the request and get the response.
            await graphServiceClient.HttpProvider.SendAsync(hrm);
            return response;
        }

        public async Task<IosManagedAppProtection> ImportPatchIosManagedAppProtectionAsync(string iosManagedAppProtection)
        {
            IosManagedAppProtection managedAppProtection = JsonConvert.DeserializeObject<IosManagedAppProtection>(iosManagedAppProtection);
            var response = await PatchIosManagedAppProtectionAsync(managedAppProtection);
            string requestUrl = graphEndpoint + "/deviceAppManagement/iosManagedAppProtections/" + response.Id + "/targetApps";

            // Restore assignment of app protection policy
            string requestBody = ConvertToApppProtectionAssignment(iosManagedAppProtection);
            HttpRequestMessage hrm = new HttpRequestMessage(new HttpMethod("PATCH"), requestUrl)
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };

            // Authenticate (add access token) our HttpRequestMessage
            await graphServiceClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);
            signalRMessage.sendMessage($"{hrm.Method}: {requestUrl}");
            // Send the request and get the response.
            await graphServiceClient.HttpProvider.SendAsync(hrm);
            return response;
        }

        public async Task<TargetedManagedAppConfiguration> ImportWindowsManagedAppProtectionAsync(string targetedManagedAppConfiguration)
        {
            TargetedManagedAppConfiguration managedAppProtection = JsonConvert.DeserializeObject<TargetedManagedAppConfiguration>(targetedManagedAppConfiguration);
            var response = await AddManagedAppConfigurationAsync(managedAppProtection);
            string requestUrl = graphEndpoint + "/deviceAppManagement/targetedManagedAppConfigurations/" + response.Id + "/targetApps";

            // Restore assignment of app protection policy
            string requestBody = ConvertToApppProtectionAssignment(targetedManagedAppConfiguration);
            HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };

            // Authenticate (add access token) our HttpRequestMessage
            await graphServiceClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);
            signalRMessage.sendMessage($"{hrm.Method}: {requestUrl}");
            // Send the request and get the response.
            await graphServiceClient.HttpProvider.SendAsync(hrm);
            return response;
        }

        public async Task<TargetedManagedAppConfiguration> ImportPatchWindowsManagedAppProtectionAsync(string targetedManagedAppConfiguration)
        {
            TargetedManagedAppConfiguration managedAppProtection = JsonConvert.DeserializeObject<TargetedManagedAppConfiguration>(targetedManagedAppConfiguration);
            var response = await PatchManagedAppConfigurationAsync(managedAppProtection);
            string requestUrl = graphEndpoint + "/deviceAppManagement/targetedManagedAppConfigurations/" + response.Id + "/targetApps";

            // Restore assignment of app protection policy
            string requestBody = ConvertToApppProtectionAssignment(targetedManagedAppConfiguration);
            HttpRequestMessage hrm = new HttpRequestMessage(new HttpMethod("PATCH"), requestUrl)
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };

            // Authenticate (add access token) our HttpRequestMessage
            await graphServiceClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);
            signalRMessage.sendMessage($"{hrm.Method}: {requestUrl}");
            // Send the request and get the response.
            await graphServiceClient.HttpProvider.SendAsync(hrm);
            return response;
        }
    }
}