// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Graph;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ModernWorkplaceConcierge.TokenStorage;
using Microsoft.Identity.Client;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Net.Http;
using ModernWorkplaceConcierge.Helpers;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Collections;

namespace ModernWorkplaceConcierge.Helpers
{
    public class GraphJson {

        [JsonProperty("@odata.type", NullValueHandling = NullValueHandling.Ignore)]
        public string OdataType { get; set; }
        [JsonProperty("@odata.context", NullValueHandling = NullValueHandling.Ignore)]
        public string OdataValue { get { return OdataType; } set { OdataType = value; } }
    }

    public static class GraphHelper
    {
        // Load configuration settings from PrivateSettings.config
        private static readonly string appId = ConfigurationManager.AppSettings["AppId"];
        private static readonly string appSecret = ConfigurationManager.AppSettings["AppSecret"];
        private static readonly string redirectUri = ConfigurationManager.AppSettings["RedirectUri"];
        private static readonly string graphScopes = ConfigurationManager.AppSettings["AppScopes"];
        private static readonly string  graphEndpoint = ConfigurationManager.AppSettings["GraphEndpoint"];

        public static async Task<string> ImportCaConfig(string policy)
        {
            ConditionalAccessPolicy conditionalAccessPolicy = JsonConvert.DeserializeObject<ConditionalAccessPolicy>(policy);

            conditionalAccessPolicy.id = null;
            conditionalAccessPolicy.state = "disabled";
            conditionalAccessPolicy.createdDateTime = null;

            string requestContent = JsonConvert.SerializeObject(conditionalAccessPolicy, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            });

            try
            {
                var success = await GraphHelper.AddConditionalAccessPolicyAsync(requestContent);

                return success.ToString();
            }
            catch
            {
                // remove Id's
                conditionalAccessPolicy.conditions.users.includeUsers = new string[] { "none" };
                conditionalAccessPolicy.conditions.users.excludeUsers = null;
                conditionalAccessPolicy.conditions.users.includeGroups = null;
                conditionalAccessPolicy.conditions.users.excludeGroups = null;
                conditionalAccessPolicy.conditions.users.includeRoles = null;
                conditionalAccessPolicy.conditions.users.excludeRoles = null;

                conditionalAccessPolicy.conditions.applications.includeApplications = new string[] { "none" };
                conditionalAccessPolicy.conditions.applications.excludeApplications = null;

                requestContent = JsonConvert.SerializeObject(conditionalAccessPolicy, new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                });

                var success = await GraphHelper.AddConditionalAccessPolicyAsync(requestContent);

                return "Unknown tenant ID's removed! \r\n" + success.ToString();   
            }
        }

        public static async Task<string> AddIntuneConfig(string result) {

            GraphJson json = JsonConvert.DeserializeObject<GraphJson>(result);

            if (json.OdataValue.Contains("CompliancePolicy"))
            {
                JObject o = JObject.Parse(result);

                JObject o2 = JObject.Parse(@"{scheduledActionsForRule:[{ruleName:'PasswordRequired',scheduledActionConfigurations:[{actionType:'block',gracePeriodHours:'0',notificationTemplateId:'',notificationMessageCCList:[]}]}]}");

                o.Add("scheduledActionsForRule", o2.SelectToken("scheduledActionsForRule"));

                string jsonPolicy = JsonConvert.SerializeObject(o);

                DeviceCompliancePolicy deviceCompliancePolicy = JsonConvert.DeserializeObject<DeviceCompliancePolicy>(jsonPolicy);

                var response = await AddDeviceCompliancePolicyAsync(deviceCompliancePolicy);

                return response.ODataType + " | " +response.DisplayName;
            }
            else if (json.OdataValue.Contains("Configuration") && json.OdataValue.Contains("windows"))
            {
                DeviceConfiguration deviceConfiguration = JsonConvert.DeserializeObject<DeviceConfiguration>(result);

                // request fails when true :(
                deviceConfiguration.SupportsScopeTags = false;

                var response = await AddDeviceConfigurationAsync(deviceConfiguration);

                return response.ODataType + " | " + response.DisplayName;
            }
            else if (json.OdataValue.Contains("deviceManagementScripts"))
            {
                DeviceManagementScript deviceManagementScript = JsonConvert.DeserializeObject<DeviceManagementScript>(result);

                // remove id - otherwise request fails
                deviceManagementScript.Id = "";

                var response = await AddDeviceManagementScriptsAsync(deviceManagementScript);

                return "#microsoft.graph.deviceManagementScript" + " | " + response.DisplayName;
            }
            else if (json.OdataValue.Contains("WindowsAutopilotDeploymentProfile"))
            {
                WindowsAutopilotDeploymentProfile windowsAutopilotDeploymentProfile = JsonConvert.DeserializeObject<WindowsAutopilotDeploymentProfile>(result);

                var response = await AddWindowsAutopilotDeploymentProfile(windowsAutopilotDeploymentProfile);

                return response.ODataType + " | " + response.DisplayName;

            }
            else if (json.OdataValue.Contains("#microsoft.graph.iosManagedAppProtection"))
            {
                IosManagedAppProtection managedAppProtection = JsonConvert.DeserializeObject<IosManagedAppProtection>(result);

                var response = await AddIosManagedAppProtectionAsync(managedAppProtection);

                string requestUrl = graphEndpoint  + "/deviceAppManagement/iosManagedAppProtections/" + response.Id + "/targetApps";

                // Try adding assigned apps fro MAM policy
                try
                {
                    string requestBody = ConvertToApppProtectionAssignment(result);

                    HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                    {
                        Content = new StringContent(requestBody, Encoding.UTF8, "application/json")

                    };

                    var graphClient = GetAuthenticatedClient();

                    // Authenticate (add access token) our HttpRequestMessage
                    await graphClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

                    // Send the request and get the response.
                    await graphClient.HttpProvider.SendAsync(hrm);
                }
                catch { }

                return "#microsoft.graph.iosManagedAppProtection | " + response.DisplayName;

            }else if (json.OdataValue.Contains("#microsoft.graph.androidManagedAppProtection"))
            {
                AndroidManagedAppProtection managedAppProtection = JsonConvert.DeserializeObject<AndroidManagedAppProtection>(result);

                var response = await AddAndroidManagedAppProtectionAsync(managedAppProtection);

                string requestUrl = graphEndpoint + "/deviceAppManagement/androidManagedAppProtections/" + response.Id + "/targetApps";

                // Try adding assigned apps fro MAM policy
                try
                {
                    string requestBody = ConvertToApppProtectionAssignment(result);

                    HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                    {
                        Content = new StringContent(requestBody, Encoding.UTF8, "application/json")

                    };

                    var graphClient = GetAuthenticatedClient();

                    // Authenticate (add access token) our HttpRequestMessage
                    await graphClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

                    // Send the request and get the response.
                    await graphClient.HttpProvider.SendAsync(hrm);
                }
                catch { }

                return "#microsoft.graph.androidManagedAppProtection | " + response.DisplayName;
            }
            else if (json.OdataValue.Contains("#microsoft.graph.targetedManagedAppConfiguration"))
            {
                TargetedManagedAppConfiguration managedAppConfiguration = JsonConvert.DeserializeObject<TargetedManagedAppConfiguration>(result);

                var response = await AddManagedAppConfigurationAsync(managedAppConfiguration);

                string requestUrl = graphEndpoint + "/deviceAppManagement/targetedManagedAppConfigurations/" + response.Id + "/targetApps";

                // Try adding assigned apps fro MAM policy
                try
                {
                    string requestBody = ConvertToApppProtectionAssignment(result);

                    HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Post, requestUrl)
                    {
                        Content = new StringContent(requestBody, Encoding.UTF8, "application/json")

                    };

                    var graphClient = GetAuthenticatedClient();

                    // Authenticate (add access token) our HttpRequestMessage
                    await graphClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

                    // Send the request and get the response.
                    await graphClient.HttpProvider.SendAsync(hrm);
                }
                catch { }

                return "#microsoft.graph.targetedManagedAppConfiguration | " + response.DisplayName;

            }
            else
            {
                return null;
            }
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

        public static async Task<string> AddConditionalAccessPolicyAsync(string ConditionalAccessPolicyJSON)
        {
            var graphClient = GetAuthenticatedClient();

            string requestUrl = graphEndpoint + "/conditionalAccess/policies";

            HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new StringContent(ConditionalAccessPolicyJSON, Encoding.UTF8, "application/json")
                            
            };

            // Authenticate (add access token) our HttpRequestMessage
            await graphClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

            // Send the request and get the response.
            HttpResponseMessage response = await graphClient.HttpProvider.SendAsync(hrm);

            return await response.Content.ReadAsStringAsync();
        }

        // Get's ESP, Enrollment restrictions, WHFB settings etc...
        public static async Task<IEnumerable<DeviceEnrollmentConfiguration>> GetDeviceEnrollmentConfigurationsAsync()
        {
            var graphClient = GetAuthenticatedClient();

            var deviceManagementScripts = await graphClient.DeviceManagement.DeviceEnrollmentConfigurations.Request().GetAsync();

            return deviceManagementScripts.CurrentPage;
        }


        public static async Task<IEnumerable<PlannerPlan>> GetplannerPlans()
        {
            var graphClient = GetAuthenticatedClient();
            var response = await graphClient.Me.Planner.Plans.Request().GetAsync();
            return response.CurrentPage;
        }

        public static async Task<User> GetUser(string displayName)
        {
            var graphClient = GetAuthenticatedClient();
            var response = await graphClient.Users.Request().Filter("displayName eq" + displayName).GetAsync();
            return response.CurrentPage.First();
        }

        public static async Task<PlannerTask> AddPlannerTask(PlannerTask plannerTask)
        {
            var graphClient = GetAuthenticatedClient();
            var response = await graphClient.Planner.Tasks.Request().AddAsync(plannerTask);
            return response;
        }

        public static async Task<PlannerTaskDetails> AddPlannerTaskDetails(PlannerTaskDetails plannerTaskDetails, string id )
        {
            var graphClient = GetAuthenticatedClient();
            var response = await graphClient.Planner.Tasks[id].Details.Request().CreateAsync(plannerTaskDetails);
            return response;
        }

        public static async Task<IEnumerable<DeviceAndAppManagementRoleAssignment>> GetRoleAssignments()
        {
            var graphClient = GetAuthenticatedClient();
            var response = await graphClient.DeviceManagement.RoleAssignments.Request().GetAsync();
            return response;
        }

        public static async Task<DeviceAndAppManagementRoleAssignment> AddRoleAssignment(DeviceAndAppManagementRoleAssignment roleAssignment)
        {
            var graphClient = GetAuthenticatedClient();
            var response = await graphClient.DeviceManagement.RoleAssignments.Request().AddAsync(roleAssignment);
            return response;
        }

        public static async Task<IEnumerable<RoleScopeTag>> GetRoleScopeTags()
        {
            var graphClient = GetAuthenticatedClient();
            var response = await graphClient.DeviceManagement.RoleScopeTags.Request().GetAsync();
            return response;
        }

        public static async Task<RoleScopeTag> AddRoleScopeTag(RoleScopeTag roleScopeTag)
        {
            var graphClient = GetAuthenticatedClient();
            var response = await graphClient.DeviceManagement.RoleScopeTags.Request().AddAsync(roleScopeTag);
            return response;
        }

        public static async Task<TargetedManagedAppConfiguration> AddManagedAppConfigurationAsync(TargetedManagedAppConfiguration managedAppConfiguration)
        {
            var graphClient = GetAuthenticatedClient();
            var response = await graphClient.DeviceAppManagement.TargetedManagedAppConfigurations.Request().AddAsync(managedAppConfiguration);
            return response;
        }

        public static async Task<IEnumerable<DeviceManagementScript>> GetDeviceManagementScriptsAsync()
        {
            var graphClient = GetAuthenticatedClient();
            var result = await graphClient.DeviceManagement.DeviceManagementScripts.Request().GetAsync();
            return result.CurrentPage;

        }

        public static async Task<IEnumerable<RoleDefinition>> GetRoleDefinitions()
        {
            var graphClient = GetAuthenticatedClient();
            var result = await graphClient.DeviceManagement.RoleDefinitions.Request().GetAsync();
            
            return result.CurrentPage;

        }

        public static async Task<RoleDefinition> CopyRoleDefinition(string Id)
        {
            var graphClient = GetAuthenticatedClient();
            RoleDefinition roleDefinition = await graphClient.DeviceManagement.RoleDefinitions[Id].Request().GetAsync();

            roleDefinition.IsBuiltIn = false;
            roleDefinition.DisplayName += "- Copy";
            roleDefinition.Id = null;

            RoleDefinition roleDefinitionCopy = await graphClient.DeviceManagement.RoleDefinitions.Request().AddAsync(roleDefinition);

            return roleDefinitionCopy;

        }

        public static async Task<IosManagedAppProtection> AddIosManagedAppProtectionAsync(IosManagedAppProtection managedAppProtection)
        {
            var graphClient = GetAuthenticatedClient();
            var response = await graphClient.DeviceAppManagement.IosManagedAppProtections.Request().AddAsync(managedAppProtection);
            return response;
        }

        public static async Task<AndroidManagedAppProtection> AddAndroidManagedAppProtectionAsync(AndroidManagedAppProtection managedAppProtection)
        {
            var graphClient = GetAuthenticatedClient();
            var response = await graphClient.DeviceAppManagement.AndroidManagedAppProtections.Request().AddAsync(managedAppProtection);
            return response;
        }

        public static async Task<DeviceManagementScript> AddDeviceManagementScriptsAsync(DeviceManagementScript deviceManagementScript)
        {
            var graphClient = GetAuthenticatedClient();
            var response = await graphClient.DeviceManagement.DeviceManagementScripts.Request().AddAsync(deviceManagementScript);
            return response;
        }

        public static async Task<DeviceManagementScript> GetDeviceManagementScriptsAsync(string Id)
        {
            var graphClient = GetAuthenticatedClient();
            DeviceManagementScript deviceManagementScript = await graphClient.DeviceManagement.DeviceManagementScripts[Id].Request().GetAsync();
            return deviceManagementScript;
        }

        public static async Task<string> GetDeviceManagementScriptRawAsync(string Id)
        {
            var graphClient = GetAuthenticatedClient();

            string requestUrl = graphEndpoint + "/deviceManagement/deviceManagementScripts/"+Id;

            HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            // Authenticate (add access token) our HttpRequestMessage
            await graphClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

            // Send the request and get the response.
            HttpResponseMessage response = await graphClient.HttpProvider.SendAsync(hrm);

            string result = await response.Content.ReadAsStringAsync(); //right!

            return result;
        }

        public static async Task<string> GetConditionalAccessPoliciesAsync()
        {
            var graphClient = GetAuthenticatedClient();

            string requestUrl = graphEndpoint + "/conditionalAccess/policies";

            HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            
            // Authenticate (add access token) our HttpRequestMessage
            await graphClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

            // Send the request and get the response.
            HttpResponseMessage response = await graphClient.HttpProvider.SendAsync(hrm);

            string result = await response.Content.ReadAsStringAsync(); //right!

            return result;
        }

        public static async Task<string> GetConditionalAccessPolicyAsync(string Id)
        {
            var graphClient = GetAuthenticatedClient();
            graphClient.BaseUrl = graphEndpoint;

            string requestUrl = graphEndpoint + "/conditionalAccess/policies/" + Id;

            HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            // Authenticate (add access token) our HttpRequestMessage
            await graphClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

            // Send the request and get the response.
            HttpResponseMessage response = await graphClient.HttpProvider.SendAsync(hrm);

            string result = await response.Content.ReadAsStringAsync(); 

            return result;
        }

        public static async Task<IEnumerable<DeviceConfiguration>> GetDeviceConfigurationsAsync()
        {
            var graphClient = GetAuthenticatedClient();
            var deviceConfigurations = await graphClient.DeviceManagement.DeviceConfigurations.Request().GetAsync();
            return deviceConfigurations.CurrentPage;
        }

        public static async Task<DeviceConfiguration> AddDeviceConfigurationAsync(DeviceConfiguration deviceConfiguration)
        {
            var graphClient = GetAuthenticatedClient();
            var result = await graphClient.DeviceManagement.DeviceConfigurations.Request().AddAsync(deviceConfiguration);
            return result;
        }

        public static async Task<IEnumerable<DeviceCompliancePolicy>> GetDeviceCompliancePoliciesAsync()
        {
            var graphClient = GetAuthenticatedClient();
            var deviceCompliancePolicies = await graphClient.DeviceManagement.DeviceCompliancePolicies.Request().GetAsync();
            return deviceCompliancePolicies.CurrentPage;
        }

        public static async Task <DeviceCompliancePolicy> AddDeviceCompliancePolicyAsync(DeviceCompliancePolicy deviceCompliancePolicy)
        {
            var graphClient = GetAuthenticatedClient();
            var result = await graphClient.DeviceManagement.DeviceCompliancePolicies.Request().AddAsync(deviceCompliancePolicy);
            return result;
        }

        public static async Task<IEnumerable<ManagedAppPolicy>> GetManagedAppProtectionAsync()
        {
            var graphClient = GetAuthenticatedClient();
            var managedAppProtection = await graphClient.DeviceAppManagement.ManagedAppPolicies.Request().GetAsync();
            return managedAppProtection.CurrentPage;
        }

        public static async Task<IEnumerable<ManagedMobileApp>> GetManagedAppProtectionAssignmentAsync(string Id)
        {
            var graphClient = GetAuthenticatedClient();
            var response = await graphClient.DeviceAppManagement.DefaultManagedAppProtections[Id].Apps.Request().GetAsync();
            return response.CurrentPage;
        }

        public static async Task<IEnumerable<ManagedMobileApp>> GetTargetedManagedAppConfigurationsAssignedAppsAsync(string Id)
        {
            var graphClient = GetAuthenticatedClient();
            var apps =  await graphClient.DeviceAppManagement.TargetedManagedAppConfigurations[Id].Apps.Request().GetAsync();
            return apps.CurrentPage;
        }

        public static async Task<ManagedAppPolicy> GetManagedAppProtectionAsync(string Id)
        {
            var graphClient = GetAuthenticatedClient();
            var managedAppProtection = await graphClient.DeviceAppManagement.IosManagedAppProtections[Id].Request().GetAsync();
            return managedAppProtection;
        }

        public static async Task <IEnumerable<WindowsAutopilotDeploymentProfile>> GetWindowsAutopilotDeploymentProfiles()
        {
            var graphClient = GetAuthenticatedClient();
            var windowsAutopilotDeploymentProfiles = await graphClient.DeviceManagement.WindowsAutopilotDeploymentProfiles.Request().GetAsync();
            return windowsAutopilotDeploymentProfiles.CurrentPage;
        }

        public static async Task<WindowsAutopilotDeploymentProfile> GetWindowsAutopilotDeploymentProfiles(string Id)
        {
            var graphClient = GetAuthenticatedClient();
            WindowsAutopilotDeploymentProfile windowsAutopilotDeploymentProfile = await graphClient.DeviceManagement.WindowsAutopilotDeploymentProfiles[Id].Request().GetAsync();
            return windowsAutopilotDeploymentProfile;
        }

        public static async Task<WindowsAutopilotDeploymentProfile> AddWindowsAutopilotDeploymentProfile(WindowsAutopilotDeploymentProfile autopilotDeploymentProfile)
        {
            var graphClient = GetAuthenticatedClient();
            var response = await graphClient.DeviceManagement.WindowsAutopilotDeploymentProfiles.Request().AddAsync(autopilotDeploymentProfile);
            return response;
        }

        public static async Task<Organization> GetOrgDetailsAsync()
        {
            var graphClient = GetAuthenticatedClient();
               
            var org =  await graphClient.Organization.Request().GetAsync();

            Organization organization = org.CurrentPage.First();

            return organization;
        }

        public static async Task<string> GetDefaultDomain()
        {
            Organization organization = await GetOrgDetailsAsync();

            string verifiedDomain = organization.VerifiedDomains.First().Name;

            foreach (VerifiedDomain domain in organization.VerifiedDomains)
            {
                if ((bool)domain.IsDefault)
                {
                    verifiedDomain = domain.Name;
                }
            }

            return verifiedDomain;
        }

        public static async Task<User> GetUserDetailsAsync(string accessToken)
        {
            var graphClient = new GraphServiceClient(
                new Microsoft.Graph.DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        requestMessage.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", accessToken);
                    }));

             return await graphClient.Me.Request().GetAsync();
        }

        private static GraphServiceClient GetAuthenticatedClient()
        {
            return new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        var idClient = ConfidentialClientApplicationBuilder.Create(appId)
                            .WithRedirectUri(redirectUri)
                            .WithClientSecret(appSecret)
                            .Build();

                        var tokenStore = new SessionTokenStore(idClient.UserTokenCache, 
                            HttpContext.Current, ClaimsPrincipal.Current);

                        var accounts = await idClient.GetAccountsAsync();

                    // By calling this here, the token can be refreshed
                    // if it's expired right before the Graph call is made
                    var scopes = graphScopes.Split(' ');
                        var result = await idClient.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                            .ExecuteAsync();

                        requestMessage.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", result.AccessToken);
                    }));
        }
    }
}