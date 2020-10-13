using System;
using Arcus.Security.Core;
using Invictus.Testing.LogicApps;
using Moq;
using Xunit;

namespace Invictus.Testing.Tests.Unit.LogicApps
{
    public class LogicAppAuthenticationTests
    {
        [Theory]
        [ClassData(typeof(Blanks))]
        public void UsingServicePrincipal_WithBlankTenantId_Throws(string tenantId)
        {
            Assert.ThrowsAny<ArgumentException>(
                () => LogicAppAuthentication.UsingServicePrincipal(tenantId, "subscription ID", "client ID", "client secret"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void UsingServicePrincipal_WithSubscriptionId_Throws(string subscriptionId)
        {
            Assert.ThrowsAny<ArgumentException>(
                () => LogicAppAuthentication.UsingServicePrincipal("tenant ID", subscriptionId, "client ID", "client secret"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void UsingServicePrincipal_WithBlankClientId_Throws(string clientId)
        {
            Assert.ThrowsAny<ArgumentException>(
                () => LogicAppAuthentication.UsingServicePrincipal("tenant Id", "subscription ID", clientId, "client secret"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void UsingServicePrincipal_WithBlankClientSecret_Throws(string clientSecret)
        {
            Assert.ThrowsAny<ArgumentException>(
                () => LogicAppAuthentication.UsingServicePrincipal("tenant ID", "subscription ID", "client ID", clientSecret));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void UsingServicePrincipalSecretProvider_WithBlankTenantId_Throws(string tenantId)
        {
            Assert.ThrowsAny<ArgumentException>(
                () => LogicAppAuthentication.UsingServicePrincipal(tenantId, "subscription ID", "client ID", "client secret", Mock.Of<ISecretProvider>()));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void UsingServicePrincipalSecretProvider_WithSubscriptionId_Throws(string subscriptionId)
        {
            Assert.ThrowsAny<ArgumentException>(
                () => LogicAppAuthentication.UsingServicePrincipal("tenant ID", subscriptionId, "client ID", "client secret", Mock.Of<ISecretProvider>()));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void UsingServicePrincipalSecretProvider_WithBlankClientId_Throws(string clientId)
        {
            Assert.ThrowsAny<ArgumentException>(
                () => LogicAppAuthentication.UsingServicePrincipal("tenant Id", "subscription ID", clientId, "client secret", Mock.Of<ISecretProvider>()));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void UsingServicePrincipalSecretProvider_WithBlankClientSecret_Throws(string clientSecret)
        {
            Assert.ThrowsAny<ArgumentException>(
                () => LogicAppAuthentication.UsingServicePrincipal("tenant ID", "subscription ID", "client ID", clientSecret, Mock.Of<ISecretProvider>()));
        }

        [Fact]
        public void UsingServicePrincipalSecretProvider_WithoutSecretProvider_Throws()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => LogicAppAuthentication.UsingServicePrincipal("tenant ID", "subscription ID", "client ID", "client secret key", secretProvider: null));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void UsingAccessToken_WithBlankSubscriptionId_Throws(string subscriptionId)
        {
            Assert.ThrowsAny<ArgumentException>(
                () => LogicAppAuthentication.UsingAccessToken(subscriptionId, "access-token"));
        }

        [Theory]
        [ClassData(typeof(Blanks))]
        public void UsingAccessToken_WithBlankAccessToken_Throws(string accessToken)
        {
            Assert.ThrowsAny<ArgumentException>(
                () => LogicAppAuthentication.UsingAccessToken("subscription ID", accessToken));
        }
    }
}
