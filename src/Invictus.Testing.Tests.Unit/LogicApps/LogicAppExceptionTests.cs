using System.Collections.Generic;
using Invictus.Testing.LogicApps;
using Xunit;

namespace Invictus.Testing.Tests.Unit.LogicApps
{
    public class LogicAppExceptionTests
    {
        [Fact]
        public void CreateException_WithAllProperties_AssignsAllProperties()
        {
            // Arrange
            string logicApp = "logic app name", resourceGroup = "resource group", subscriptionId = "subscription ID";
            string message = "There's something wrong with the logic app";
            var innerException = new KeyNotFoundException("Couldn't find the logic app");

            // Act
            var exception = new LogicAppException(subscriptionId, resourceGroup, logicApp, message, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(logicApp, exception.LogicAppName);
            Assert.Equal(resourceGroup, exception.ResourceGroup);
            Assert.Equal(subscriptionId, exception.SubscriptionId);
            Assert.Equal(innerException, exception.InnerException);
        }
    }
}
