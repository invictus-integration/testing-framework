using System;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Xunit;

namespace Invictus.Testing.Tests.Unit.LogicApps
{
    public class AzureCloudExtensionsTests
    {
        [Theory]
        [InlineData((AzureCloud) 5)]
        [InlineData(AzureCloud.China | AzureCloud.USGovernment)]
        public void GetAzureEnvironment_WithOutOfBoundsEnum_Throws(AzureCloud cloud)
        {
            Assert.ThrowsAny<ArgumentException>(() => cloud.GetAzureEnvironment());
        }
    }
}
