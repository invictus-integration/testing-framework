using System;

// ReSharper disable once CheckNamespace
namespace Microsoft.Azure.Management.ResourceManager.Fluent
{
    /// <summary>
    /// Extensions on the <see cref="AzureCloud"/>.
    /// </summary>
    public static class AzureCloudExtensions
    {
        /// <summary>
        /// Gets the <see cref="AzureEnvironment"/> representation of the <see cref="AzureCloud"/>.
        /// </summary>
        /// <param name="cloud">The enumeration to determine the environment.</param>
        /// <returns>
        ///     An <see cref="AzureEnvironment"/> instance that the <see cref="AzureCloud"/> represents.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="cloud"/> is outside the bounds of the enumeration.</exception>
        public static AzureEnvironment GetAzureEnvironment(this AzureCloud cloud)
        {
            switch (cloud)
            {
                case AzureCloud.Global:       return AzureEnvironment.AzureGlobalCloud;
                case AzureCloud.China:        return AzureEnvironment.AzureChinaCloud;;
                case AzureCloud.USGovernment: return AzureEnvironment.AzureUSGovernment;
                case AzureCloud.German:       return AzureEnvironment.AzureGermanCloud;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cloud), cloud, "Unknown cloud environment");
            }
        }
    }
}
