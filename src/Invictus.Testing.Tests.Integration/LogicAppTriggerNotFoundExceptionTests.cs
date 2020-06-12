using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace Invictus.Testing.Tests.Integration
{
    public class LogicAppTriggerNotFoundExceptionTests
    {
        [Fact]
        public void CreateException_WithAllProperties_AssignsAllProperties()
        {
            // Arrange
            string logicApp = "logic app name", resourceGroup = "resource group", subscriptionId = "subscription ID";
            string message = "There's something wrong with finding the trigger in the logic app";
            var innerException = new KeyNotFoundException("Couldn't find the trigger in the logic app");

            // Act
            var exception = new LogicAppTriggerNotFoundException(message, logicApp, resourceGroup, subscriptionId, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Equal(logicApp, exception.LogicAppName);
            Assert.Equal(resourceGroup, exception.ResourceGroup);
            Assert.Equal(subscriptionId, exception.SubscriptionId);
            Assert.Equal(innerException, exception.InnerException);
        }

        [Fact]
        public void SerializeException_WithoutProperties_SerializesWithoutProperties()
        {
            // Arrange
            var innerException = new KeyNotFoundException("No trigger with this key found");
            var exception = new LogicAppTriggerNotFoundException("Trigger could not be found", innerException);

            var expected = exception.ToString();

            // Act
            LogicAppTriggerNotFoundException actual = SerializeDeserializeException(exception);

            // Assert
            Assert.Equal(expected, actual.ToString());
        }

        [Fact]
        public void SerializeException_WithProperties_SerializeWithProperties()
        {
            // Arrange
            string logicApp = "logic app name",
                   resourceGroup = "resource group",
                   subscriptionId = "subscription ID";

            var innerException = new KeyNotFoundException("No trigger with this key found");
            var exception = new LogicAppTriggerNotFoundException("Trigger could not be found", logicApp, resourceGroup, subscriptionId, innerException);

            string expected = exception.ToString();

            // Act
            LogicAppTriggerNotFoundException actual = SerializeDeserializeException(exception);

            // Assert
            Assert.Equal(expected, actual.ToString());
            Assert.Equal(logicApp, actual.LogicAppName);
            Assert.Equal(resourceGroup, actual.ResourceGroup);
            Assert.Equal(subscriptionId, actual.SubscriptionId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithBlankLogicAppName_Fails(string logicAppName)
        {
            Assert.Throws<ArgumentException>(
                () => new LogicAppTriggerNotFoundException("Trigger could not be found", logicAppName, "resource group", "subscription ID"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithBlankResourceGroup_Fails(string resourceGroup)
        {
            Assert.Throws<ArgumentException>(
                () => new LogicAppTriggerNotFoundException("Trigger could not be found", "logic app", resourceGroup, "subscription ID"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithBlankSubscriptionId_Fails(string subscriptionId)
        {
            Assert.Throws<ArgumentException>(
                () => new LogicAppTriggerNotFoundException("Trigger could not be found", "logic app", "resource group", subscriptionId));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ConstructorInnerException_WithBlankLogicAppName_Fails(string logicAppName)
        {
            var innerException = new Exception("The cause of the exception");
            Assert.Throws<ArgumentException>(
                () => new LogicAppTriggerNotFoundException("Trigger could not be found", logicAppName, "resource group", "subscription ID", innerException));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ConstructorInnerException_WithBlankResourceGroup_Fails(string resourceGroup)
        {
            var innerException = new Exception("The cause of the exception");
            Assert.Throws<ArgumentException>(
                () => new LogicAppTriggerNotFoundException("Trigger could not be found", "logic app", resourceGroup, "subscription ID", innerException));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ConstructorInnerException_WithBlankSubscriptionId_Fails(string subscriptionId)
        {
            var innerException = new Exception("The cause of the exception");
            Assert.Throws<ArgumentException>(
                () => new LogicAppTriggerNotFoundException("Trigger could not be found", "logic app", "resource group", subscriptionId, innerException));
        }

        private static LogicAppTriggerNotFoundException SerializeDeserializeException(LogicAppTriggerNotFoundException exception)
        {
            var formatter = new BinaryFormatter();
            using (var contents = new MemoryStream())
            {
                formatter.Serialize(contents, exception);
                contents.Seek(0, 0);

                var deserialized = (LogicAppTriggerNotFoundException) formatter.Deserialize(contents);
                return deserialized;
            }
        }
    }
}
