using Microsoft.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<DeviceCompliancePolicy> AddDeviceCompliancePolicyAsync(DeviceCompliancePolicy deviceCompliancePolicy)
        {
            var resource = graphServiceClient.DeviceManagement.DeviceCompliancePolicies.Request();
            signalRMessage.sendMessage($"POST: {resource.RequestUrl}");
            var result = await resource.AddAsync(deviceCompliancePolicy);
            signalRMessage.sendMessage($"Success: added {result.ODataType} '{result.DisplayName}'");
            return result;
        }

        public async Task<DeviceConfiguration> AddDeviceConfigurationAsync(DeviceConfiguration deviceConfiguration)
        {
            var resource = graphServiceClient.DeviceManagement.DeviceConfigurations.Request();
            signalRMessage.sendMessage($"POST: {resource.RequestUrl}");
            var result = await resource.AddAsync(deviceConfiguration);
            signalRMessage.sendMessage($"Success: added {result.ODataType} '{result.DisplayName}'");
            return result;
        }

        public async Task<DeviceManagementScript> AddDeviceManagementScriptAsync(DeviceManagementScript deviceManagementScript)
        {
            deviceManagementScript.Id = null;
            var resource = graphServiceClient.DeviceManagement.DeviceManagementScripts.Request();
            signalRMessage.sendMessage($"POST: {resource.RequestUrl}");
            var response = await resource.AddAsync(deviceManagementScript);
            signalRMessage.sendMessage($"Success: added {response.ODataType} '{response.DisplayName}'");
            return response;
        }

        public async Task<IosManagedAppProtection> AddIosManagedAppProtectionAsync(IosManagedAppProtection managedAppProtection)
        {
            var resource = graphServiceClient.DeviceAppManagement.IosManagedAppProtections.Request();
            signalRMessage.sendMessage($"POST: {resource.RequestUrl}");
            var response = await resource.AddAsync(managedAppProtection);
            return response;
        }

        public async Task<TargetedManagedAppConfiguration> AddManagedAppConfigurationAsync(TargetedManagedAppConfiguration managedAppConfiguration)
        {
            var resource = graphServiceClient.DeviceAppManagement.TargetedManagedAppConfigurations.Request();
            signalRMessage.sendMessage($"POST: {resource.RequestUrl}");
            var response = await resource.AddAsync(managedAppConfiguration);
            return response;
        }

        public async Task<WindowsInformationProtection> AddMdmWindowsInformationProtectionsAsync(MdmWindowsInformationProtectionPolicy mdmWindowsInformationProtectionPolicy)
        {
            var resource = graphServiceClient.DeviceAppManagement.MdmWindowsInformationProtectionPolicies.Request();
            signalRMessage.sendMessage($"POST: {resource.RequestUrl}");
            var mdmWindowsInformationProtection = await resource.AddAsync(mdmWindowsInformationProtectionPolicy);
            signalRMessage.sendMessage($"Success: added {mdmWindowsInformationProtection.ODataType} '{mdmWindowsInformationProtection.DisplayName}'");
            return mdmWindowsInformationProtection;
        }

        public async Task<WindowsAutopilotDeploymentProfile> AddWindowsAutopilotDeploymentProfile(WindowsAutopilotDeploymentProfile autopilotDeploymentProfile)
        {
            var resource = graphServiceClient.DeviceManagement.WindowsAutopilotDeploymentProfiles.Request();
            signalRMessage.sendMessage($"POST: {resource.RequestUrl}");
            var response = await resource.AddAsync(autopilotDeploymentProfile);
            signalRMessage.sendMessage($"Success: added {response.ODataType} '{response.DisplayName}'");
            return response;
        }

        public async Task<WindowsInformationProtection> AddWindowsInformationProtectionsAsync(WindowsInformationProtectionPolicy windowsInformationProtectionPolicy)
        {
            var resource = graphServiceClient.DeviceAppManagement.WindowsInformationProtectionPolicies.Request();
            signalRMessage.sendMessage($"POST: {resource.RequestUrl}");
            var windowsInformationProtection = await resource.AddAsync(windowsInformationProtectionPolicy);
            signalRMessage.sendMessage($"Success: added {windowsInformationProtection.ODataType} '{windowsInformationProtection.DisplayName}'");
            return windowsInformationProtectionPolicy;
        }

        public async Task<IEnumerable<GroupPolicyConfiguration>> GetGroupPolicyConfigurationsAsync()
        {
            var resource = graphServiceClient.DeviceManagement.GroupPolicyConfigurations.Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            var groupPolicyConfigurations = await resource.GetAsync();
            return groupPolicyConfigurations.CurrentPage;
        }

        public async Task<IEnumerable<GroupPolicyDefinitionValue>> GetGroupPolicyDefinitionValuesAsync(string id)
        {
            var resource = graphServiceClient.DeviceManagement.GroupPolicyConfigurations[id].DefinitionValues.Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            var groupPolicyConfigurations = await resource.GetAsync();
            return groupPolicyConfigurations.CurrentPage;
        }

        public async Task<IEnumerable<GroupPolicyPresentation>> GetGroupPolicyPresentationValuesAsync(string id)
        {
            var resource = graphServiceClient.DeviceManagement.GroupPolicyDefinitions[id].Presentations.Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            var groupPolicyPresentations = await resource.GetAsync();
            return groupPolicyPresentations.CurrentPage;
        }

        public async Task<IEnumerable<AndroidManagedAppProtection>> GetAndroidManagedAppProtectionsAsync()
        {
            var resource = graphServiceClient.DeviceAppManagement.AndroidManagedAppProtections.Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            var androidManagedAppProtection = await resource.GetAsync();
            return androidManagedAppProtection.CurrentPage;
        }

        public async Task<IEnumerable<DeviceCompliancePolicy>> GetDeviceCompliancePoliciesAsync()
        {
            var resource = graphServiceClient.DeviceManagement.DeviceCompliancePolicies.Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            var deviceCompliancePolicies = await resource.GetAsync();
            return deviceCompliancePolicies.CurrentPage;
        }

        public async Task<IEnumerable<MobileApp>> GetMobileAppsAsync()
        {
            var resource = graphServiceClient.DeviceAppManagement.MobileApps.Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            var deviceConfigurations = await resource.GetAsync();
            return deviceConfigurations.CurrentPage;
        }

        public async Task<Win32LobAppPowerShellScriptDetection> GetWin32MobileAppPowerShellDetectionRuleAsync(string id)
        {
            var resource = graphServiceClient.DeviceAppManagement.MobileApps[id].Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            Win32LobApp app = (Win32LobApp) await resource.GetAsync();

            Win32LobAppPowerShellScriptDetection powerShellScriptDetection = app.DetectionRules.Where(rule => rule.ODataType.Equals("#microsoft.graph.win32LobAppPowerShellScriptDetection")).Cast<Win32LobAppPowerShellScriptDetection>().First();
            signalRMessage.sendMessage(JsonConvert.SerializeObject(powerShellScriptDetection));
            return powerShellScriptDetection;
        }

        public async Task<Win32LobApp> GetWin32MobileAppAsync(string id)
        {
            var resource = graphServiceClient.DeviceAppManagement.MobileApps[id].Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            Win32LobApp app = (Win32LobApp)await resource.GetAsync();
            return app;
        }

        public async Task<IEnumerable<Win32LobApp>> GetWin32MobileAppsAsync()
        {
            var resource = graphServiceClient.DeviceAppManagement.MobileApps.Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            var apps = await resource.Filter("isOf('microsoft.graph.win32LobApp')").GetAsync();

            return apps.Cast<Win32LobApp>();
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

        public async Task<IEnumerable<IosManagedAppProtection>> GetIosManagedAppProtectionsAsync()
        {
            var resource = graphServiceClient.DeviceAppManagement.IosManagedAppProtections.Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            var iosManagedAppProtection = await resource.GetAsync();
            return iosManagedAppProtection.CurrentPage;
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

        public async Task<IEnumerable<TargetedManagedAppConfiguration>> GetTargetedManagedAppConfigurationsAsync()
        {
            var resource = graphServiceClient.DeviceAppManagement.TargetedManagedAppConfigurations.Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            var targetedManagedAppConfig = await resource.GetAsync();
            return targetedManagedAppConfig.CurrentPage;
        }

        public async Task<ManagedAppPolicy> GetManagedAppProtectionAsync(string Id)
        {
            var resource = graphServiceClient.DeviceAppManagement.IosManagedAppProtections[Id].Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            var managedAppProtection = await resource.GetAsync();
            return managedAppProtection;
        }

        public async Task<IEnumerable<WindowsInformationProtection>> GetMdmWindowsInformationProtectionsAsync()
        {
            var resource = graphServiceClient.DeviceAppManagement.MdmWindowsInformationProtectionPolicies.Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            var mdmWindowsInformationProtectionPolicies = await resource.GetAsync();
            return mdmWindowsInformationProtectionPolicies.CurrentPage;
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

        public async Task<IEnumerable<ManagedDeviceMobileAppConfiguration>> GetManagedDeviceMobileAppConfigurationsAsync()
        {
            var resource = graphServiceClient.DeviceAppManagement.MobileAppConfigurations.Request();
            signalRMessage.sendMessage($"GET: {resource.RequestUrl}");
            var managedApps = await resource.GetAsync();
            return managedApps.CurrentPage;
        }

        public async Task<ManagedDeviceMobileAppConfiguration> AddManagedDeviceMobileAppConfigurationAsync(ManagedDeviceMobileAppConfiguration managedDeviceMobileAppConfiguration)
        {
            var resource = graphServiceClient.DeviceAppManagement.MobileAppConfigurations.Request();
            signalRMessage.sendMessage($"POST: {resource.RequestUrl}");
            await resource.AddAsync(managedDeviceMobileAppConfiguration);
            signalRMessage.sendMessage($"Success: added {managedDeviceMobileAppConfiguration.ODataType} '{managedDeviceMobileAppConfiguration.DisplayName}'");
            return managedDeviceMobileAppConfiguration;
        }

        public async Task<ManagedDeviceMobileAppConfiguration> PatchManagedDeviceMobileAppConfigurationAsync(ManagedDeviceMobileAppConfiguration managedDeviceMobileAppConfiguration)
        {
            var resource = graphServiceClient.DeviceAppManagement.MobileAppConfigurations[managedDeviceMobileAppConfiguration.Id].Request();
            signalRMessage.sendMessage($"PATCH: {resource.RequestUrl}");
            var managedApps = await resource.UpdateAsync(managedDeviceMobileAppConfiguration);
            signalRMessage.sendMessage($"Success: updated {managedDeviceMobileAppConfiguration.ODataType} ({managedDeviceMobileAppConfiguration.DisplayName})");
            return managedDeviceMobileAppConfiguration;
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
            signalRMessage.sendMessage($"Success: added {response.ODataType} '{response.DisplayName}'");
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

            signalRMessage.sendMessage($"Success: added {response.ODataType} '{response.DisplayName}'");
            return response;
        }

        public async Task<AndroidManagedAppProtection> ImportPatchAndroidManagedAppProtectionAsync(string androidManagedAppProtection)
        {
            AndroidManagedAppProtection managedAppProtection = JsonConvert.DeserializeObject<AndroidManagedAppProtection>(androidManagedAppProtection);
            await PatchAndroidManagedAppProtectionAsync(managedAppProtection);
            string requestUrl = graphEndpoint + "/deviceAppManagement/androidManagedAppProtections/" + managedAppProtection.Id + "/targetApps";

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
            var response = await graphServiceClient.HttpProvider.SendAsync(hrm);

            if (response.IsSuccessStatusCode)
            {
                signalRMessage.sendMessage($"Success: updated {managedAppProtection.ODataType} ({managedAppProtection.DisplayName})");
            }

            return managedAppProtection;
        }

        public async Task<IosManagedAppProtection> ImportPatchIosManagedAppProtectionAsync(string iosManagedAppProtection)
        {
            IosManagedAppProtection managedAppProtection = JsonConvert.DeserializeObject<IosManagedAppProtection>(iosManagedAppProtection);
            await PatchIosManagedAppProtectionAsync(managedAppProtection);
            string requestUrl = graphEndpoint + "/deviceAppManagement/iosManagedAppProtections/" + managedAppProtection.Id + "/targetApps";

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
            var response = await graphServiceClient.HttpProvider.SendAsync(hrm);

            if (response.IsSuccessStatusCode)
            {
                signalRMessage.sendMessage($"Success: updated {managedAppProtection.ODataType} ({managedAppProtection.DisplayName})");
            }

            return managedAppProtection;
        }

        public async Task<TargetedManagedAppConfiguration> ImportPatchTargetedManagedAppConfigurationAsync(string targetedManagedAppConfiguration)
        {
            TargetedManagedAppConfiguration managedAppProtection = JsonConvert.DeserializeObject<TargetedManagedAppConfiguration>(targetedManagedAppConfiguration);
            await PatchManagedAppConfigurationAsync(managedAppProtection);
            string requestUrl = graphEndpoint + "/deviceAppManagement/targetedManagedAppConfigurations/" + managedAppProtection.Id + "/targetApps";

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
            var response = await graphServiceClient.HttpProvider.SendAsync(hrm);

            if (response.IsSuccessStatusCode)
            {
                signalRMessage.sendMessage($"Success: updated {managedAppProtection.ODataType} ({managedAppProtection.DisplayName})");
            }

            return managedAppProtection;
        }

        public async Task<TargetedManagedAppConfiguration> ImportTargetedManagedAppConfigurationAsync(string targetedManagedAppConfiguration)
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
            signalRMessage.sendMessage($"Success: added {response.ODataType} '{response.DisplayName}'");
            return response;
        }

        public async Task<AndroidManagedAppProtection> PatchAndroidManagedAppProtectionAsync(AndroidManagedAppProtection managedAppProtection)
        {
            var resource = graphServiceClient.DeviceAppManagement.AndroidManagedAppProtections[managedAppProtection.Id].Request();
            signalRMessage.sendMessage($"PATCH: {resource.RequestUrl}");
            var response = await resource.UpdateAsync(managedAppProtection);
            return response;
        }
        public async Task<DeviceCompliancePolicy> PatchDeviceCompliancePolicyAsync(DeviceCompliancePolicy deviceCompliancePolicy)
        {
            var resource = graphServiceClient.DeviceManagement.DeviceCompliancePolicies[deviceCompliancePolicy.Id].Request();
            signalRMessage.sendMessage($"PATCH: {resource.RequestUrl}");
            await resource.UpdateAsync(deviceCompliancePolicy);
            signalRMessage.sendMessage($"Success: updated {deviceCompliancePolicy.ODataType} '{deviceCompliancePolicy.DisplayName}'");
            return deviceCompliancePolicy;
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

            signalRMessage.sendMessage($"Success: updated {deviceConfiguration.ODataType} '{deviceConfiguration.DisplayName}'");
            return deviceConfiguration;
        }
        public async Task<DeviceManagementScript> PatchDeviceManagementScriptAsync(DeviceManagementScript deviceManagementScript)
        {
            deviceManagementScript.LastModifiedDateTime = null;
            deviceManagementScript.CreatedDateTime = null;
            var resource = graphServiceClient.DeviceManagement.DeviceManagementScripts[deviceManagementScript.Id].Request();
            signalRMessage.sendMessage($"PATCH: {resource.RequestUrl}");
            var response = await resource.UpdateAsync(deviceManagementScript);
            signalRMessage.sendMessage($"Success: updated {response.ODataType} '{response.DisplayName}'");
            return response;
        }
        public async Task<IosManagedAppProtection> PatchIosManagedAppProtectionAsync(IosManagedAppProtection managedAppProtection)
        {
            var resource = graphServiceClient.DeviceAppManagement.IosManagedAppProtections[managedAppProtection.Id].Request();
            signalRMessage.sendMessage($"PATCH: {resource.RequestUrl}");
            var response = await resource.UpdateAsync(managedAppProtection);
            return response;
        }
        public async Task<TargetedManagedAppConfiguration> PatchManagedAppConfigurationAsync(TargetedManagedAppConfiguration managedAppConfiguration)
        {
            var resource = graphServiceClient.DeviceAppManagement.TargetedManagedAppConfigurations[managedAppConfiguration.Id].Request();
            signalRMessage.sendMessage($"PATCH: {resource.RequestUrl}");
            var response = await resource.UpdateAsync(managedAppConfiguration);
            return response;
        }
        public async Task<WindowsAutopilotDeploymentProfile> PatchWindowsAutopilotDeploymentProfile(WindowsAutopilotDeploymentProfile autopilotDeploymentProfile)
        {
            var resource = graphServiceClient.DeviceManagement.WindowsAutopilotDeploymentProfiles[autopilotDeploymentProfile.Id].Request();
            signalRMessage.sendMessage($"PATCH: {resource.RequestUrl}");
            var response = await resource.UpdateAsync(autopilotDeploymentProfile);
            signalRMessage.sendMessage($"Success: updated {autopilotDeploymentProfile.ODataType} '{autopilotDeploymentProfile.DisplayName}'");
            return response;
        }
    }
}