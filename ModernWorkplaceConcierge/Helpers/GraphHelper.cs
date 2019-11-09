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
using IntuneConcierge.Helpers;

namespace ModernWorkplaceConcierge.Helpers
{
    public static class GraphHelper
    {
        // Load configuration settings from PrivateSettings.config
        private static string appId = ConfigurationManager.AppSettings["ida:AppId"];
        private static string appSecret = ConfigurationManager.AppSettings["ida:AppSecret"];
        private static string redirectUri = ConfigurationManager.AppSettings["ida:RedirectUri"];
        private static string graphScopes = ConfigurationManager.AppSettings["ida:AppScopes"];
        private static string graphEndpoint = ConfigurationManager.AppSettings["ida:GraphEndpoint"];


        // Get's ESP, Enrollment restrictions, WHFB settings etc...
        public static async Task<IEnumerable<DeviceEnrollmentConfiguration>> GetDeviceEnrollmentConfigurationsAsync()
        {
            var graphClient = GetAuthenticatedClient();

            var deviceManagementScripts = await graphClient.DeviceManagement.DeviceEnrollmentConfigurations.Request().GetAsync();

            return deviceManagementScripts.CurrentPage;
        }

        public static async Task<IEnumerable<DeviceManagementScript>> GetDeviceManagementScriptsAsync()
        {
            var graphClient = GetAuthenticatedClient();

            var result = await graphClient.DeviceManagement.DeviceManagementScripts.Request().GetAsync();

            return result.CurrentPage;

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

        public static async Task<IEnumerable<DeviceCompliancePolicy>> GetDeviceCompliancePoliciesAsync()
        {
            var graphClient = GetAuthenticatedClient();

            var deviceCompliancePolicies = await graphClient.DeviceManagement.DeviceCompliancePolicies.Request().GetAsync();

            return deviceCompliancePolicies.CurrentPage;
        }

        public static async Task<IEnumerable<ManagedAppPolicy>> GetManagedAppProtectionAsync()
        {
            var graphClient = GetAuthenticatedClient();
            
            var managedAppProtection = await graphClient.DeviceAppManagement.IosManagedAppProtections.Request().GetAsync();

            return managedAppProtection.CurrentPage;
        }

        public static async Task<ManagedAppPolicy> GetManagedAppProtectionAsync(string Id)
        {
            var graphClient = GetAuthenticatedClient();

            var managedAppProtection = await graphClient.DeviceAppManagement.IosManagedAppProtections[Id].Request().GetAsync();

            return managedAppProtection;
        }

        public static async Task <IEnumerable<Microsoft.Graph.WindowsAutopilotDeploymentProfile>> GetWindowsAutopilotDeploymentProfiles()
        {
            var graphClient = GetAuthenticatedClient();

            var windowsAutopilotDeploymentProfiles = await graphClient.DeviceManagement.WindowsAutopilotDeploymentProfiles.Request().GetAsync();

            return windowsAutopilotDeploymentProfiles.CurrentPage;
        }

        public static async Task<Microsoft.Graph.WindowsAutopilotDeploymentProfile> GetWindowsAutopilotDeploymentProfiles(string Id)
        {
            var graphClient = GetAuthenticatedClient();

            Microsoft.Graph.WindowsAutopilotDeploymentProfile windowsAutopilotDeploymentProfile = await graphClient.DeviceManagement.WindowsAutopilotDeploymentProfiles[Id].Request().GetAsync();

            return windowsAutopilotDeploymentProfile;
        }

        public static async Task<Organization> GetOrgDetailsAsync()
        {
            var graphClient = GetAuthenticatedClient();
               
            var org =  await graphClient.Organization.Request().GetAsync();

            Organization organization = org.CurrentPage.First();

            return organization;
        }

        public static async Task<User> GetUserDetailsAsync(string accessToken)
        {
            var graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
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