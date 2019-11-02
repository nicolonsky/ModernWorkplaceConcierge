// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Graph;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IntuneConcierge.TokenStorage;
using Microsoft.Identity.Client;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Web;

namespace IntuneConcierge.Helpers
{
    public static class GraphHelper
    {
        // Load configuration settings from PrivateSettings.config
        private static string appId = ConfigurationManager.AppSettings["ida:AppId"];
        private static string appSecret = ConfigurationManager.AppSettings["ida:AppSecret"];
        private static string redirectUri = ConfigurationManager.AppSettings["ida:RedirectUri"];
        private static string graphScopes = ConfigurationManager.AppSettings["ida:AppScopes"];
        private static string graphEndpoint = ConfigurationManager.AppSettings["ida:GraphEndpoint"];
        
        public static async Task<IEnumerable<DeviceConfiguration>> GetDeviceConfigurationsAsync()
        {
            var graphClient = GetAuthenticatedClient();
            graphClient.BaseUrl = graphEndpoint;

            var events = await graphClient.DeviceManagement.DeviceConfigurations.Request().GetAsync();
                
            return events.CurrentPage;
        }

        public static async Task<IEnumerable<DeviceCompliancePolicy>> GetDeviceCompliancePoliciesAsync()
        {
            var graphClient = GetAuthenticatedClient();
            graphClient.BaseUrl = graphEndpoint;

            var events = await graphClient.DeviceManagement.DeviceCompliancePolicies.Request().GetAsync();

            return events.CurrentPage;
        }

        public static async Task<IEnumerable<ManagedAppPolicy>> GetManagedAppProtectionAsync()
        {
            var graphClient = GetAuthenticatedClient();
            graphClient.BaseUrl = graphEndpoint;

            var events = await graphClient.DeviceAppManagement.ManagedAppPolicies.Request().GetAsync();

            return events.CurrentPage;
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