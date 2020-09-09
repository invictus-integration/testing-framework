using GuardNet;
using System.Net.Http;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System;

using ISecretProvider = Arcus.Security.Core.ISecretProvider;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Codit.Testing.ResourceApi
{
    /// <summary>
    /// Authentication representation to authenticate with resources running on Azure.
    /// </summary>
    public class ResourceApiAuthentication
    {
        private readonly Func<Task<string>> _authenticateAsync;
        private ResourceApiAuthentication(Func<Task<string>> authenticateAsync)
        {
            Guard.NotNull(authenticateAsync, nameof(authenticateAsync));

            _authenticateAsync = authenticateAsync;
        }

        /// <summary>
        /// Uses the service principal to authenticate with Azure.
        /// </summary>
        /// <param name="tenantId">The ID where the resources are located on Azure.</param>
        /// <param name="subscriptionId">The ID that identifies the subscription on Azure.</param>
        /// <param name="clientId">The ID of the client or application that has access to the logic apps running on Azure.</param>
        /// <param name="clientSecretKey">The secret of the client or application that has access to the logic apps running on Azure.</param>
        /// <param name="secretProvider">The provider to get the client secret; using the <paramref name="clientSecretKey"/>.</param>
        public static ResourceApiAuthentication UsingServicePrincipal(string tenantId, string subscriptionId, string clientId, string clientSecretKey, ISecretProvider secretProvider)
        {
            Guard.NotNullOrWhitespace(tenantId, nameof(tenantId));
            Guard.NotNullOrWhitespace(subscriptionId, nameof(subscriptionId));
            Guard.NotNullOrWhitespace(clientId, nameof(clientId));
            Guard.NotNullOrWhitespace(clientSecretKey, nameof(clientSecretKey));
            Guard.NotNull(secretProvider, nameof(secretProvider));

            return new ResourceApiAuthentication(async () =>
            {
                string clientSecret = await secretProvider.GetRawSecretAsync(clientSecretKey);
                var managementClient = await AuthenticateResourceManagerAsync(subscriptionId, tenantId, clientId, clientSecret);
                return managementClient;
            });
        }

        /// <summary>
        /// Uses the service principal to authenticate with Azure.
        /// </summary>
        /// <param name="tenantId">The ID where the resources are located on Azure.</param>
        /// <param name="subscriptionId">The ID that identifies the subscription on Azure.</param>
        /// <param name="clientId">The ID of the client or application that has access to the logic apps running on Azure.</param>
        /// <param name="clientSecret">The secret of the client or application that has access to the logic apps running on Azure.</param>
        public static ResourceApiAuthentication UsingServicePrincipal(string tenantId, string subscriptionId, string clientId, string clientSecret)
        {
            Guard.NotNullOrWhitespace(tenantId, nameof(tenantId));
            Guard.NotNullOrWhitespace(subscriptionId, nameof(subscriptionId));
            Guard.NotNullOrWhitespace(clientId, nameof(clientId));
            Guard.NotNullOrWhitespace(clientSecret, nameof(clientSecret));

            return new ResourceApiAuthentication(
                () => AuthenticateResourceManagerAsync(subscriptionId, tenantId, clientId, clientSecret));
        }

        /// <summary>
        /// Uses the service principal to authenticate with Azure.
        /// </summary>
        /// <param name="tenantId">The ID where the resources are located on Azure.</param>
        /// <param name="subscriptionId">The ID that identifies the subscription on Azure.</param>
        /// <param name="clientId">The ID of the client or application that has access to the logic apps running on Azure.</param>
        /// <param name="clientSecret">The secret of the client or application that has access to the logic apps running on Azure.</param>
        /// <param name="resource">The resource string for Auth context.</param> 
        /// <param name="authUri">The authUri context.</param>  
        public static ResourceApiAuthentication UsingServicePrincipal(string tenantId, string subscriptionId, string clientId, string clientSecret, string resource, string authUri)
        {
            Guard.NotNullOrWhitespace(tenantId, nameof(tenantId));
            Guard.NotNullOrWhitespace(clientId, nameof(clientId));
            Guard.NotNullOrWhitespace(clientSecret, nameof(clientSecret));
            Guard.NotNullOrWhitespace(subscriptionId, nameof(authUri));
            Guard.NotNullOrWhitespace(subscriptionId, nameof(resource));

            string authority = string.Format(CultureInfo.InvariantCulture, authUri, tenantId);

            return new ResourceApiAuthentication(
                () => AccessTokenUmt(clientId, clientSecret, resource, authority));
        }
/// <summary>
         /// Authenticate with Azure with the previously chosen authentication mechanism.
         /// </summary>
         /// <returns>
         ///     The management client to interact with logic app resources running on Azure.
         /// </returns>
        public async Task<string> AuthenticateAsync()
        {
            return await _authenticateAsync();
        }

        private static Task<string> AccessTokenUmt(string clientId, string clientSecret, string adAppId, string authContext)
        {
            Task<string> token = Task<string>.Factory.StartNew(() =>
            {
                var clientCredential = new ClientCredential(clientId, clientSecret);
                AuthenticationContext context = new AuthenticationContext(authContext, false);
                AuthenticationResult authenticationResult = context.AcquireTokenAsync(adAppId, clientCredential).Result;

                return authenticationResult.AccessToken;
            });
            return token;
        }

        private static async Task<string> AuthenticateResourceManagerAsync(string subscriptionId, string tenantId, string clientId, string clientSecret)
        {
            string baseAddress = string.Format(CultureInfo.InvariantCulture, "https://login.microsoftonline.com/{0}/oauth2/token", tenantId);
            string resource = "https://management.azure.com/";
            string grant_type = "client_credentials";

            var form = new Dictionary<string, string>
                {
                    {"grant_type", grant_type},
                    {"client_id", clientId},
                    {"client_secret", clientSecret},
                    {"resource", resource},
                };

            var httpClient = new System.Net.Http.HttpClient();
            HttpResponseMessage tokenResponse = await httpClient.PostAsync(baseAddress, new FormUrlEncodedContent(form));
            var jsonContent = await tokenResponse.Content.ReadAsStringAsync();
            dynamic data = JObject.Parse(jsonContent);
            var token = ((Newtonsoft.Json.Linq.JValue)((Newtonsoft.Json.Linq.JProperty)((Newtonsoft.Json.Linq.JContainer)data).Last).Value).Value;
            return token.ToString();
        }
    }
}
