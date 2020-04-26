using Microsoft.Graph;
using Microsoft.Identity.Client;
using ModernWorkplaceConcierge.TokenStorage;
using System.Configuration;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Web;

namespace ModernWorkplaceConcierge.Helpers
{
    public class GraphClient
    {
        // Load configuration settings from PrivateSettings.config
        protected static readonly string appId = ConfigurationManager.AppSettings["AppId"];

        protected static readonly string appSecret = ConfigurationManager.AppSettings["AppSecret"];
        protected static readonly string redirectUri = ConfigurationManager.AppSettings["RedirectUri"];
        protected static readonly string graphScopes = ConfigurationManager.AppSettings["AppScopes"];
        public static readonly string graphEndpoint = ConfigurationManager.AppSettings["GraphEndpoint"];

        protected GraphServiceClient GetAuthenticatedClient()
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
    }
}