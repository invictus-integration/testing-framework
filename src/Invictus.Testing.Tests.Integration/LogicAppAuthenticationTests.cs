using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Arcus.Security.Core;
using GuardNet;
using Invictus.Testing.LogicApps;
using Microsoft.Azure.Management.Logic;
using Xunit;
using Xunit.Abstractions;

namespace Invictus.Testing.Tests.Integration
{
    public class LogicAppAuthenticationTests : IntegrationTest, ISecretProvider
    {
        private const string ClientSecretKey = "CLIENT_SECRET";

        private bool _isClientSecretRequested;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationTest"/> class.
        /// </summary>
        public LogicAppAuthenticationTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        [Fact]
        public async Task AuthenticateLogicAppManagement_UsingSecretProvider_Succeeds()
        {
            // Arrange
            string subscriptionId = Configuration.GetAzureSubscriptionId();
            string tenantId = Configuration.GetAzureTenantId();
            string clientId = Configuration.GetAzureClientId();
            var authentication = LogicAppAuthentication.UsingServicePrincipal(tenantId, subscriptionId, clientId, ClientSecretKey, this);

            // Act
            using (LogicManagementClient managementClient = await authentication.AuthenticateAsync())
            {
                // Assert
                Assert.NotNull(managementClient);
            }

            Assert.True(_isClientSecretRequested);
        }

        public Task<string> GetRawSecretAsync(string secretName)
        {
            Guard.For<ArgumentException>(() => !secretName.Equals(ClientSecretKey), "Should request for correct client secret key");

            _isClientSecretRequested = true;
            string secret = Configuration.GetAzureClientSecret();
            return Task.FromResult(secret);
        }

        public async Task<Secret> GetSecretAsync(string secretName)
        {
            throw new NotImplementedException();
        }
    }
}
