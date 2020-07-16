using Microsoft.AspNet.SignalR;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using ModernWorkplaceConcierge.TokenStorage;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace ModernWorkplaceConcierge.Helpers
{
    public static class GraphHelper
    {
        // Load configuration settings from PrivateSettings.config
        private static readonly string appId = ConfigurationManager.AppSettings["AppId"];
        private static readonly string appSecret = ConfigurationManager.AppSettings["AppSecret"];
        private static readonly string redirectUri = ConfigurationManager.AppSettings["RedirectUri"];
        private static readonly string graphScopes = ConfigurationManager.AppSettings["AppScopes"];
        private static readonly string graphEndpoint = ConfigurationManager.AppSettings["GraphEndpoint"];

        public static async Task<User> GetUser(string displayName, string clientId = null)
        {
            var graphClient = GetAuthenticatedClient();

            if (!string.IsNullOrEmpty(clientId))
            {
                var hubContext = GlobalHost.ConnectionManager.GetHubContext<MwHub>();
                hubContext.Clients.Client(clientId).addMessage("GET: " + graphClient.Users.Request().Filter($"startsWith(displayName,'{displayName}')").RequestUrl);
            }

            var response = await graphClient
                    .Users
                    .Request()
                    .Filter($"startsWith(displayName,'{displayName}')")
                    .GetAsync();

            return response.CurrentPage.First();
        }

        public static async Task<Group> CreateGroup(string displayName, string clientId = null)
        {
            var graphClient = GetAuthenticatedClient();

            if (!string.IsNullOrEmpty(clientId))
            {
                var hubContext = GlobalHost.ConnectionManager.GetHubContext<MwHub>();
                hubContext.Clients.Client(clientId).addMessage("POST: " + graphClient.Groups.Request().RequestUrl);
            }

            // Check if group not already exists
            try
            {
                var check = await graphClient.Groups.Request().Filter($"displayName eq '{displayName}'").GetAsync();

                if (check.FirstOrDefault() != null && check.FirstOrDefault().Id != null)
                {
                    if (!string.IsNullOrEmpty(clientId))
                    {
                        var hubContext = GlobalHost.ConnectionManager.GetHubContext<MwHub>();
                        hubContext.Clients.Client(clientId).addMessage("Warning AAD Group with name: '" + displayName + "' already exists!");
                    }
                    return check.CurrentPage.FirstOrDefault();
                }
            }
            catch (System.Exception)
            {
            }

            Group group = new Group();
            group.SecurityEnabled = true;
            group.DisplayName = displayName;
            group.Description = "Created with the ModernWorkplaceConcierge";
            group.MailEnabled = false;
            group.MailNickname = displayName;

            var respGroup = await graphClient.Groups.Request().AddAsync(group);

            return respGroup;
        }

        public static async Task<Group> GetGroup(string Id, string clientId = null)
        {
            var graphClient = GetAuthenticatedClient();

            if (!string.IsNullOrEmpty(clientId))
            {
                var hubContext = GlobalHost.ConnectionManager.GetHubContext<MwHub>();
                hubContext.Clients.Client(clientId).addMessage("GET: " + graphClient.Groups[Id].Request().RequestUrl);
            }

            var group = await graphClient.Groups[Id].Request().GetAsync();

            return group;
        }

        public static async Task<IEnumerable<DirectoryRoleTemplate>> GetDirectoryRoleTemplates(string clientId = null)
        {
            var graphClient = GetAuthenticatedClient();

            if (!string.IsNullOrEmpty(clientId))
            {
                var hubContext = GlobalHost.ConnectionManager.GetHubContext<MwHub>();
                hubContext.Clients.Client(clientId).addMessage("GET: " + graphClient.DirectoryRoleTemplates.Request().RequestUrl);
            }

            var roles = await graphClient.DirectoryRoleTemplates.Request().GetAsync();

            return roles.CurrentPage;
        }

        public static async Task<IEnumerable<ServicePrincipal>> GetServicePrincipals(string clientId = null)
        {
            var graphClient = GetAuthenticatedClient();

            if (!string.IsNullOrEmpty(clientId))
            {
                var hubContext = GlobalHost.ConnectionManager.GetHubContext<MwHub>();
                hubContext.Clients.Client(clientId).addMessage("GET: " + graphClient.ServicePrincipals.Request().RequestUrl);
            }

            var servicePrincipals = await graphClient.ServicePrincipals.Request().GetAsync();

            return servicePrincipals.CurrentPage;
        }

        public static async Task<User> GetUserById(string Id, string clientId = null)
        {
            var graphClient = GetAuthenticatedClient();

            if (!string.IsNullOrEmpty(clientId))
            {
                var hubContext = GlobalHost.ConnectionManager.GetHubContext<MwHub>();
                hubContext.Clients.Client(clientId).addMessage("GET: " + graphClient.Users[Id].Request().RequestUrl);
            }

            var user = await graphClient.Users[Id].Request().GetAsync();

            return user;
        }

        public static async Task<Organization> GetOrgDetailsAsync(string clientId = null)
        {
            var graphClient = GetAuthenticatedClient();

            if (!string.IsNullOrEmpty(clientId))
            {
                var hubContext = GlobalHost.ConnectionManager.GetHubContext<MwHub>();
                hubContext.Clients.Client(clientId).addMessage("GET: " + graphClient.Organization.Request().RequestUrl);
            }

            var org = await graphClient.Organization.Request().GetAsync();

            Organization organization = org.CurrentPage.First();

            return organization;
        }

        public static async Task<string> GetDefaultDomain(string clientId = null)
        {
            Organization organization = await GetOrgDetailsAsync(clientId);

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
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        requestMessage.Headers.Authorization =
                            new AuthenticationHeaderValue("Bearer", accessToken);
                    }));

            return await graphClient.Me.Request().GetAsync();
        }

        public static async Task<byte[]> GetUserPhotoAsync(string accessToken)
        {
            var graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        requestMessage.Headers.Authorization =
                           new AuthenticationHeaderValue("Bearer", accessToken);
                    }));

            var content = await graphClient.Me.Photo.Content.Request().GetAsync();

            byte[] bytes = new byte[content.Length];
            content.Read(bytes, 0, (int)content.Length);
            return bytes;
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