using Microsoft.AspNet.SignalR;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using ModernWorkplaceConcierge.TokenStorage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ModernWorkplaceConcierge.Helpers
{
    public class GraphConditionalAccess
    {
        // Load configuration settings from PrivateSettings.config
        private static readonly string appId = ConfigurationManager.AppSettings["AppId"];
        private static readonly string appSecret = ConfigurationManager.AppSettings["AppSecret"];
        private static readonly string redirectUri = ConfigurationManager.AppSettings["RedirectUri"];
        private static readonly string graphScopes = ConfigurationManager.AppSettings["AppScopes"];
        private static readonly string graphEndpoint = ConfigurationManager.AppSettings["GraphEndpoint"];

        private GraphServiceClient graphServiceClient;
        private string clientId;
        private SignalRMessage signalRMessage;

        public GraphConditionalAccess (string clientId)
        {
            this.graphServiceClient = GetAuthenticatedClient();
            this.clientId = clientId;
            this.signalRMessage = new SignalRMessage(clientId);
        }

        private Microsoft.Graph.GraphServiceClient GetAuthenticatedClient()
        {
            return new Microsoft.Graph.GraphServiceClient(
                new Microsoft.Graph.DelegateAuthenticationProvider(
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
                    }
                )
            );
        }

        public async Task<IEnumerable<NamedLocation>> GetNamedLocationsAsync(string clientId = null)
        {
            signalRMessage.sendMessage("GET: " + graphServiceClient.ConditionalAccess.NamedLocations.Request().RequestUrl);
            var namedLocations = await graphServiceClient.ConditionalAccess.NamedLocations.Request().GetAsync();
            return namedLocations.CurrentPage;
        }

        public async Task<ConditionalAccessPolicy> AddConditionalAccessPolicyAsync(ConditionalAccessPolicy conditionalAccessPolicy, string clientId = null)
        {

            // Following properties need to be disabled for successful POST
            conditionalAccessPolicy.id = null;
            conditionalAccessPolicy.state = "disabled";
            conditionalAccessPolicy.createdDateTime = null;
            conditionalAccessPolicy.modifiedDateTime = null;

            string requestUrl = graphEndpoint + "/identity/conditionalAccess/policies";
            HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = new StringContent(JsonConvert.SerializeObject(conditionalAccessPolicy, new JsonSerializerSettings()
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                }), Encoding.UTF8, "application/json")

            };

            if (conditionalAccessPolicy.conditions.deviceStates != null)
            {
                signalRMessage.sendMessage("Warning device states are currently not imported by the Graph API, you need to enable them manually on the policy!");
            }

            if (conditionalAccessPolicy.sessionControls != null && conditionalAccessPolicy.sessionControls.applicationEnforcedRestrictions != null)
            {
                signalRMessage.sendMessage("Warning you need to enable Exchange online and SharePoint online for app enforced restrictions!");
            }

            // Authenticate (add access token) our HttpRequestMessage
            await this.graphServiceClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);
            signalRMessage.sendMessage($"{hrm.Method}: {requestUrl}");

            // Send the request and get the response.
            HttpResponseMessage response = await graphServiceClient.HttpProvider.SendAsync(hrm);
            ConditionalAccessPolicy conditionalAccessPolicyResult = JsonConvert.DeserializeObject<ConditionalAccessPolicy>(await response.Content.ReadAsStringAsync());

            return conditionalAccessPolicyResult;
        }

        public async Task<HttpResponseMessage> PatchConditionalAccessPolicyAsync(ConditionalAccessPolicy conditionalAccessPolicy, string clientId = null)
        {
            conditionalAccessPolicy.createdDateTime = null;
            conditionalAccessPolicy.modifiedDateTime = null;

            string requestUrl = graphEndpoint + $"/identity/conditionalAccess/policies/{conditionalAccessPolicy.id}";

            HttpRequestMessage hrm = new HttpRequestMessage(new HttpMethod("PATCH"), requestUrl)
            {
                Content = new StringContent(JsonConvert.SerializeObject(conditionalAccessPolicy), Encoding.UTF8, "application/json")
            };

            // Authenticate (add access token) our HttpRequestMessage
            await graphServiceClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);
            signalRMessage.sendMessage($"{hrm.Method}: {requestUrl}");

            // Send the request and get the response.
            HttpResponseMessage response = await graphServiceClient.HttpProvider.SendAsync(hrm);

            if (response.IsSuccessStatusCode)
            {
                signalRMessage.sendMessage($"Success: updated CA policy: {conditionalAccessPolicy.displayName} ({conditionalAccessPolicy.id})");
            }

            return response;
        }

        public async Task<ConditionalAccessPolicy> TryAddConditionalAccessPolicyAsync(ConditionalAccessPolicy conditionalAccessPolicy)
        {
            try
            {
                var response = await AddConditionalAccessPolicyAsync(conditionalAccessPolicy);
                return response;

            }
            catch
            {
                signalRMessage.sendMessage("Discarding tenant specific information for CA policy: '" + conditionalAccessPolicy.displayName + "'");
                
                // remove Id's
                conditionalAccessPolicy.conditions.users.includeUsers = new string[] { "none" };
                conditionalAccessPolicy.conditions.users.excludeUsers = null;
                conditionalAccessPolicy.conditions.users.includeGroups = null;
                conditionalAccessPolicy.conditions.users.excludeGroups = null;
                conditionalAccessPolicy.conditions.users.includeRoles = null;
                conditionalAccessPolicy.conditions.users.excludeRoles = null;

                conditionalAccessPolicy.conditions.applications.includeApplications = new string[] { "none" };
                conditionalAccessPolicy.conditions.applications.excludeApplications = null;

                var response = await AddConditionalAccessPolicyAsync(conditionalAccessPolicy);
                return response;
            }
        }

        public async Task<IEnumerable<ConditionalAccessPolicy>> GetConditionalAccessPoliciesAsync(string clientId = null)
        {

            string requestUrl = graphEndpoint + "/identity/conditionalAccess/policies";
            HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            // Authenticate (add access token) our HttpRequestMessage
            await graphServiceClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

            signalRMessage.sendMessage("GET: " + requestUrl);

            // Send the request and get the response.
            HttpResponseMessage response = await graphServiceClient.HttpProvider.SendAsync(hrm);
            string result = await response.Content.ReadAsStringAsync();

            ConditionalAccessPolicies conditionalAccessPolicies = JsonConvert.DeserializeObject<ConditionalAccessPolicies>(result);

            return conditionalAccessPolicies.Value;
        }
    }
}