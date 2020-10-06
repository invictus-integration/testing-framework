using System;
using System.Threading.Tasks;
using Arcus.Security.Core;
using GuardNet;
using Microsoft.Azure.Management.Logic;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;

namespace Invictus.Testing.LogicApps 
{
    /// <summary>
    /// Authentication representation to authenticate with logic apps running on Azure.
    /// </summary>
    public class LogicAppAuthentication
    {
        private readonly Func<Task<LogicManagementClient>> _authenticateAsync;

        private LogicAppAuthentication(Func<Task<LogicManagementClient>> authenticateAsync)
        {
            Guard.NotNull(authenticateAsync, nameof(authenticateAsync), "Requires a function that will authenticate with Azure to interact with Logic Apps running on Azure");

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
        /// <param name="cloud">The Azure cloud environment to use during authenticating and interacting with the Azure Logic Apps resources.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="tenantId"/>, <paramref name="subscriptionId"/>, <paramref name="clientId"/>, or <paramref name="clientSecretKey"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="secretProvider"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="cloud"/> is outside the bounds of the enumeration.</exception>
        public static LogicAppAuthentication UsingServicePrincipal(string tenantId, string subscriptionId, string clientId, string clientSecretKey, ISecretProvider secretProvider, AzureCloud cloud = AzureCloud.Global)
        {
            Guard.NotNullOrWhitespace(tenantId, nameof(tenantId), "Requires an tenant ID where the Azure resources are located");
            Guard.NotNullOrWhitespace(subscriptionId, nameof(subscriptionId), "Requires an ID that identifies the Azure subscription that has access to the Azure resources");
            Guard.NotNullOrWhitespace(clientId, nameof(clientId), "Requires an client or application ID that is authorized to interact with the Logic Apps running on Azure");
            Guard.NotNullOrWhitespace(clientSecretKey, nameof(clientSecretKey), "Requires an client or application secret key that points to the secret of the client or application that is authorized to interact with the Logic Apps running on Azure");
            Guard.NotNull(secretProvider, nameof(secretProvider), "Requires an secret provider instance to retrieve the secret of the client or application that is authorized to interact with the Logic Apps running on Azure");
            Guard.For(() => !Enum.IsDefined(typeof(AzureCloud), cloud), 
                new ArgumentOutOfRangeException(nameof(cloud), cloud, "Requires the Azure cloud environment to be within the bounds of the enumeration"));

            return new LogicAppAuthentication(async () =>
            {
                string clientSecret = await secretProvider.GetRawSecretAsync(clientSecretKey);
                LogicManagementClient managementClient = await AuthenticateLogicAppsManagementAsync(subscriptionId, tenantId, clientId, clientSecret, cloud);

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
        /// <param name="cloud">The Azure cloud environment to use during authenticating and interacting with the Azure Logic Apps resources.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="tenantId"/>, <paramref name="subscriptionId"/>, <paramref name="clientId"/>, or <paramref name="clientSecret"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="cloud"/> is outside the bounds of the enumeration.</exception>
        public static LogicAppAuthentication UsingServicePrincipal(string tenantId, string subscriptionId, string clientId, string clientSecret, AzureCloud cloud = AzureCloud.Global)
        {
            Guard.NotNullOrWhitespace(tenantId, nameof(tenantId), "Requires an tenant ID where the Azure resources are located");
            Guard.NotNullOrWhitespace(subscriptionId, nameof(subscriptionId), "Requires an ID that identifies the Azure subscription that has access to the Azure resources");
            Guard.NotNullOrWhitespace(clientId, nameof(clientId), "Requires an client or application ID that is authorized to interact with the Logic Apps running on Azure");
            Guard.NotNullOrWhitespace(clientSecret, nameof(clientSecret), "Requires an client or application secret that is authorized to interact with the Logic Apps running on Azure");
            Guard.For(() => !Enum.IsDefined(typeof(AzureCloud), cloud), 
                new ArgumentOutOfRangeException(nameof(cloud), cloud, "Requires the Azure cloud environment to be within the bounds of the enumeration"));

            return new LogicAppAuthentication(
                () => AuthenticateLogicAppsManagementAsync(subscriptionId, tenantId, clientId, clientSecret, cloud));
        }

        /// <summary>
        /// Uses a accessToken to authenticate with Azure.
        /// </summary>
        /// <param name="subscriptionId">The ID that identifies the subscription on Azure.</param>
        /// <param name="accessToken">The token to use to call the Azure management API.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="subscriptionId"/> or <paramref name="accessToken"/> is blank.</exception>
        public static LogicAppAuthentication UsingAccessToken(string subscriptionId, string accessToken)
        {
            Guard.NotNullOrWhitespace(subscriptionId, nameof(subscriptionId), "Requires an ID that identifies the Azure subscription that has access to the Azure resources");
            Guard.NotNullOrWhitespace(accessToken, nameof(accessToken), "Requires an access token to authenticate a client or application with Azure that is authorized to interact with the Logic Apps running on Azure");

            return new LogicAppAuthentication(() =>
            {
                LogicManagementClient client = AuthenticateLogicAppsManagement(subscriptionId, accessToken);
                return Task.FromResult(client);
            });
        }

        /// <summary>
        /// Uses an access token to authenticate with Azure.
        /// </summary>
        /// <param name="subscriptionId">The ID that identifies the subscription on Azure.</param>
        /// <param name="accessTokenKey">The secret key to use to fetch access token from the secret provider. This will be used to call the Azure management API.</param>
        /// <param name="secretProvider">The provider to get the client secret; using the <paramref name="accessTokenKey"/>.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="subscriptionId"/> or <paramref name="accessTokenKey"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="secretProvider"/> is blank.</exception>
        public static LogicAppAuthentication UsingAccessToken(string subscriptionId, string accessTokenKey, ISecretProvider secretProvider)
        {
            Guard.NotNullOrWhitespace(subscriptionId, nameof(subscriptionId), "Requires an ID that identifies the Azure subscription that has access to the Azure resources");
            Guard.NotNullOrWhitespace(accessTokenKey, nameof(accessTokenKey), "Requires an access token secret key that points to the access token of the client or application that is authorized to interact with the Logic Apps running on Azure");
            Guard.NotNull(secretProvider, nameof(secretProvider), "Requires an secret provider instance to retrieve the access token of the client or application that is authorized to interact with the Logic Apps running on Azure");

            return new LogicAppAuthentication(async () =>
            {
                string accessToken = await secretProvider.GetRawSecretAsync(accessTokenKey);
                LogicManagementClient managementClient = AuthenticateLogicAppsManagement(subscriptionId, accessToken);

                return managementClient;
            });
        }

        /// <summary>
        /// Authenticate with Azure with the previously chosen authentication mechanism.
        /// </summary>
        /// <returns>
        ///     The management client to interact with logic app resources running on Azure.
        /// </returns>
        public async Task<LogicManagementClient> AuthenticateAsync()
        {
            return await _authenticateAsync();
        }

        private static async Task<LogicManagementClient> AuthenticateLogicAppsManagementAsync(
            string subscriptionId, 
            string tenantId, 
            string clientId, 
            string clientSecret, 
            AzureCloud cloud)
        {
            AzureEnvironment azureEnvironment = cloud.GetAzureEnvironment();

            string authority = azureEnvironment.AuthenticationEndpoint + tenantId;
            var authContext = new AuthenticationContext(authority);
            var credential = new ClientCredential(clientId, clientSecret);

            AuthenticationResult token = await authContext.AcquireTokenAsync(azureEnvironment.ManagementEndpoint, credential);
            LogicManagementClient client = AuthenticateLogicAppsManagement(subscriptionId, token.AccessToken);

            return client;
        }

        private static LogicManagementClient AuthenticateLogicAppsManagement(string subscriptionId, string accessToken)
        {
            Guard.NotNullOrWhitespace(subscriptionId, nameof(subscriptionId), "Requires an ID that identifies the Azure subscription that has access to the Azure resources");
            Guard.NotNullOrWhitespace(accessToken, nameof(accessToken), "Requires an access token to authenticate a client or application with Azure that is authorized to interact with the Logic Apps running on Azure");

            return new LogicManagementClient(new TokenCredentials(accessToken))
            {
                SubscriptionId = subscriptionId
            };
        }
    }
}
