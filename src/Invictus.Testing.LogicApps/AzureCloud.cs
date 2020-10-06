

// ReSharper disable once CheckNamespace
namespace Microsoft.Azure.Management.ResourceManager.Fluent
{
    /// <summary>
    /// Represents the Azure cloud environment, used for authentication...
    /// </summary>
    public enum AzureCloud
    {
        /// <summary>
        /// Use the global Azure cloud environment, alias for <see cref="AzureEnvironment.AzureGlobalCloud"/>.
        /// </summary>
        Global = 0,

        /// <summary>
        /// Use the China Azure cloud environment, alias for <see cref="AzureEnvironment.AzureChinaCloud"/>.
        /// </summary>
        China = 1,

        /// <summary>
        /// Use the US Government cloud environment, alias for <see cref="AzureEnvironment.AzureUSGovernment"/>.
        /// </summary>
        USGovernment = 2,

        /// <summary>
        /// Use the German cloud environment, alias for <see cref="AzureEnvironment.AzureGermanCloud"/>.
        /// </summary>
        German = 4
    }
}
