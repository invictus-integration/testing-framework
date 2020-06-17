using System;
using System.Collections.Generic;
using System.IO;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Invictus.Testing.Tests.Integration 
{
    /// <summary>
    /// Test implementation of the <see cref="IConfigurationRoot"/> with the required integration test values loaded.
    /// </summary>
    public class TestConfig : IConfigurationRoot
    {
        private readonly IConfigurationRoot _configuration;

        private TestConfig(IConfigurationRoot configuration)
        {
            Guard.NotNull(configuration, nameof(configuration));

            _configuration = configuration;
        }

        /// <summary>
        /// The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationProvider" />s for this configuration.
        /// </summary>
        public IEnumerable<IConfigurationProvider> Providers => _configuration.Providers;

        /// <summary>
        /// Creates a new <see cref="IConfiguration"/> implementation with the test values loaded.
        /// </summary>
        public static TestConfig Create()
        {
            IConfigurationRoot configuration = 
                new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddJsonFile("appsettings.local.json", optional: true)
                    .Build();

            return new TestConfig(configuration);
        }

        /// <summary>
        /// Gets the Azure Logic App definition to test update interactions.
        /// </summary>
        public string GetLogicAppDefinition()
        {
            var fileName = "rcv-trigger-http.json";
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "LogicAppDefinitions", fileName);
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("No Azure Logic App definition file can be found at the expected file location", filePath);
            }

            string definition = File.ReadAllText(filePath);
            return definition;
        }

        /// <summary>
        /// Gets the name of the Azure Logic App to interact without state changes.
        /// </summary>
        public string GetTestLogicAppName()
        {
            return GetRequiredValue("Azure:LogicApps:TestLogicAppName");
        }

        /// <summary>
        /// Gets the name of the Azure Logic App to interact with state changes.
        /// </summary>
        public string GetTestMockingLogicAppName()
        {
            return GetRequiredValue("Azure:LogicApps:TestMockingLogicAppName");
        }

        /// <summary>
        /// Gets the name of the Azure Logic App resource running on Azure to test for updated workflow definitions.
        /// </summary>
        public string GetTestUpdateLogicAppName()
        {
            return GetRequiredValue("Azure:LogicApps:TestUpdateLogicAppName");
        }

        /// <summary>
        /// Gets the identifier of the Azure subscription.
        /// </summary>
        public string GetAzureSubscriptionId()
        {
            return GetRequiredValue("Azure:SubscriptionId");
        }

        /// <summary>
        /// Gets the Azure resource group where the Logic Apps are located.
        /// </summary>
        public string GetAzureResourceGroup()
        {
            return GetRequiredValue("Azure:ResourceGroup");
        }

        /// <summary>
        /// Gets the identifier of the Azure tenant where the Logic Apps are located.
        /// </summary>
        public string GetAzureTenantId()
        {
            return GetRequiredValue("Azure:TenantId");
        }

        /// <summary>
        /// Gets the identifier of the application registered to authenticate with the Azure Logic Apps.
        /// </summary>
        public string GetAzureClientId()
        {
            return GetRequiredValue("Azure:Authentication:ClientId");
        }

        /// <summary>
        /// Gets the secret of the application registered to authenticate with the Azure Logic Apps.
        /// </summary>
        public string GetAzureClientSecret()
        {
            return GetRequiredValue("Azure:Authentication:ClientSecret");
        }

        private string GetRequiredValue(string key)
        {
            string value = _configuration[key];
            if (String.IsNullOrEmpty(value))
            {
                throw new KeyNotFoundException(
                    $"Cannot find configured test value for key: '{key}'");
            }

            return value;
        }

        /// <summary>
        /// Gets a configuration sub-section with the specified key.
        /// </summary>
        /// <param name="key">The key of the configuration section.</param>
        /// <returns>The <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" />.</returns>
        /// <remarks>
        ///     This method will never return <c>null</c>. If no matching sub-section is found with the specified key,
        ///     an empty <see cref="T:Microsoft.Extensions.Configuration.IConfigurationSection" /> will be returned.
        /// </remarks>
        public IConfigurationSection GetSection(string key)
        {
            return _configuration.GetSection(key);
        }

        /// <summary>
        /// Gets the immediate descendant configuration sub-sections.
        /// </summary>
        /// <returns>The configuration sub-sections.</returns>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return _configuration.GetChildren();
        }

        /// <summary>
        /// Returns a <see cref="T:Microsoft.Extensions.Primitives.IChangeToken" /> that can be used to observe when this configuration is reloaded.
        /// </summary>
        /// <returns>A <see cref="T:Microsoft.Extensions.Primitives.IChangeToken" />.</returns>
        public IChangeToken GetReloadToken()
        {
            return _configuration.GetReloadToken();
        }

        /// <summary>Gets or sets a configuration value.</summary>
        /// <param name="key">The configuration key.</param>
        /// <returns>The configuration value.</returns>
        public string this[string key]
        {
            get => _configuration[key];
            set => _configuration[key] = value;
        }

        /// <summary>
        /// Force the configuration values to be reloaded from the underlying <see cref="T:Microsoft.Extensions.Configuration.IConfigurationProvider" />s.
        /// </summary>
        public void Reload()
        {
            _configuration.Reload();
        }
    }
}