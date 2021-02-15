using Microsoft.Graph;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ModernWorkplaceConcierge.Helpers
{
    public class GraphConditionalAccess : GraphClient
    {
        private GraphServiceClient graphServiceClient;
        private string clientId;
        private SignalRMessage signalRMessage;

        public GraphConditionalAccess(string clientId)
        {
            this.graphServiceClient = GetAuthenticatedClient();
            this.clientId = clientId;
            this.signalRMessage = new SignalRMessage(clientId);
        }

        public async Task<IEnumerable<NamedLocation>> GetNamedLocationsAsync(string clientId = null)
        {
            signalRMessage.sendMessage("GET: " + graphServiceClient.Identity.ConditionalAccess.NamedLocations.Request().RequestUrl);
            var namedLocations = await graphServiceClient.Identity.ConditionalAccess.NamedLocations.Request().GetAsync();
            return namedLocations.CurrentPage;
        }

        public async Task<ConditionalAccessPolicy> AddConditionalAccessPolicyAsync(ConditionalAccessPolicy conditionalAccessPolicy)
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

            if (conditionalAccessPolicy.sessionControls != null && conditionalAccessPolicy.sessionControls.applicationEnforcedRestrictions != null)
            {
                signalRMessage.sendMessage("Warning you need to configure Exchange online and SharePoint online for app enforced restrictions!");
            }

            // Authenticate (add access token) our HttpRequestMessage
            await this.graphServiceClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);
            signalRMessage.sendMessage($"{hrm.Method}: {requestUrl}");

            // Send the request and get the response.
            HttpResponseMessage response = await graphServiceClient.HttpProvider.SendAsync(hrm);

            ConditionalAccessPolicy conditionalAccessPolicyResult = JsonConvert.DeserializeObject<ConditionalAccessPolicy>(await response.Content.ReadAsStringAsync());

            if (response.IsSuccessStatusCode)
            {
                signalRMessage.sendMessage($"Success: created CA policy: '{conditionalAccessPolicyResult.displayName}' ({conditionalAccessPolicyResult.id})");
            }

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
            string requestUrl = $"{graphEndpoint}/identity/conditionalAccess/policies";

            HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Get, requestUrl);

            // Authenticate (add access token) our HttpRequestMessage
            await graphServiceClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

            signalRMessage.sendMessage($"{hrm.Method}:  { requestUrl}");

            // Send the request and get the response.
            HttpResponseMessage response = await graphServiceClient.HttpProvider.SendAsync(hrm);
            string result = await response.Content.ReadAsStringAsync();

            ConditionalAccessPolicies conditionalAccessPolicies = JsonConvert.DeserializeObject<ConditionalAccessPolicies>(result);

            return conditionalAccessPolicies.Value;
        }

        public async Task ClearConditonalAccessPolicies()
        {
            var policies = await GetConditionalAccessPoliciesAsync();

            foreach (ConditionalAccessPolicy policy in policies)
            {
                string requestUrl = $"{graphEndpoint}/identity/conditionalAccess/policies/{policy.id}";

                HttpRequestMessage hrm = new HttpRequestMessage(HttpMethod.Delete, requestUrl);

                signalRMessage.sendMessage($"{hrm.Method}:  { requestUrl}");

                // Authenticate (add access token) our HttpRequestMessage
                await graphServiceClient.AuthenticationProvider.AuthenticateRequestAsync(hrm);

                // Send the request and get the response.
                HttpResponseMessage response = await graphServiceClient.HttpProvider.SendAsync(hrm);
            }
        }
    }
}