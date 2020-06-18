using Arcus.Testing.Logging;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Invictus.Testing.Tests.Integration
{
    /// <summary>
    /// Provides set of reusable information required for the logic app integration tests.
    /// </summary>
    public abstract class IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationTest"/> class.
        /// </summary>
        protected IntegrationTest(ITestOutputHelper outputWriter)
        {
            Logger = new XunitTestLogger(outputWriter);
            ResourceGroup = Configuration.GetAzureResourceGroup();
            LogicAppName = Configuration.GetTestLogicAppName();
            LogicAppMockingName = Configuration.GetTestMockingLogicAppName();
            TestBaseLogicAppName = Configuration.GetTestBaseLogicAppName();

            string subscriptionId = Configuration.GetAzureSubscriptionId();
            string tenantId = Configuration.GetAzureTenantId();
            string clientId = Configuration.GetAzureClientId();
            string clientSecret = Configuration.GetAzureClientSecret();
            Authentication = LogicAuthentication.UsingServicePrincipal(tenantId, subscriptionId, clientId, clientSecret);
        }

        /// <summary>
        /// Gets the logger to write diagnostic messages during tests.
        /// </summary>
        protected ILogger Logger { get; }
        
        /// <summary>
        /// Gets the configuration available in the current integration test suite.
        /// </summary>
        protected TestConfig Configuration { get; } = TestConfig.Create();

        /// <summary>
        /// Gets the resource group where the logic app resources on Azure are located.
        /// </summary>
        protected string ResourceGroup { get; }
        
        /// <summary>
        /// Gets the name of the Azure Logic App resource running on Azure to test stateless operations against.
        /// </summary>
        protected string LogicAppName { get; }
        
        /// <summary>
        /// Gets the name of the Azure Logic App resource running on Azure to test stateful operations against.
        /// </summary>
        protected string LogicAppMockingName { get; }
        
        /// <summary>
        /// Gets the name of the Azure Logic App resource running on Azure to test for basic operations.
        /// </summary>
        protected string TestBaseLogicAppName { get; }

        /// <summary>
        /// Gets the authentication mechanism to authenticate with Azure.
        /// </summary>
        protected LogicAuthentication Authentication { get; }
    }
}
