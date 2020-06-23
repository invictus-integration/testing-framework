﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Invictus.Testing.LogicApps;
using Xunit;

namespace Invictus.Testing.Tests.Integration
{
    public class LogicAppNotUpdatedExceptionTests
    {
        [Fact]
        public void CreateException_WithAllProperties_AssignsAllProperties()
        {
            // Arrange
            string logicApp = "logic app name", resourceGroup = "resource group", subscriptionId = "subscription ID";
            string message = "There's something wrong with updating the logic app";
            var innerException = new KeyNotFoundException("Couldn't find the logic app");

            // Act
            var exception = new LogicAppNotUpdatedException(subscriptionId, resourceGroup, logicApp, message, innerException);

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
            var innerException = new InvalidOperationException("Problem with update");
            var exception = new LogicAppNotUpdatedException("App not updated", innerException);

            var expected = exception.ToString();

            // Act
            LogicAppNotUpdatedException actual = SerializeDeserializeException(exception);

            // Assert
            Assert.Equal(expected, actual.ToString());
        }

        [Fact]
        public void SerializeException_WithProperties_SerializeWithProperties()
        {
            // Arrange
            string logicApp = "logic app name",
                   resourceGroup = "resouce group",
                   subscriptionId = "subscription ID";

            var innerException = new KeyNotFoundException("Problem with update");
            var exception = new LogicAppNotUpdatedException(subscriptionId, resourceGroup, logicApp, "App not updated", innerException);

            string expected = exception.ToString();

            // Act
            LogicAppNotUpdatedException actual = SerializeDeserializeException(exception);

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
                () => new LogicAppNotUpdatedException("subscription ID", "resource group", logicAppName, "App not updated"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithBlankResourceGroup_Fails(string resourceGroup)
        {
            Assert.Throws<ArgumentException>(
                () => new LogicAppNotUpdatedException("subscription ID", resourceGroup, "logic app", "App not updated"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithBlankSubscriptionId_Fails(string subscriptionId)
        {
            Assert.Throws<ArgumentException>(
                () => new LogicAppNotUpdatedException(subscriptionId, "resource group", "logic app", "App not updated"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ConstructorInnerException_WithBlankLogicAppName_Fails(string logicAppName)
        {
            var innerException = new Exception("The cause of the exception");
            Assert.Throws<ArgumentException>(
                () => new LogicAppNotUpdatedException("subscription ID", "resource group", logicAppName, "App not updated", innerException));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ConstructorInnerException_WithBlankResourceGroup_Fails(string resourceGroup)
        {
            var innerException = new Exception("The cause of the exception");
            Assert.Throws<ArgumentException>(
                () => new LogicAppNotUpdatedException("subscription ID", resourceGroup, "logic app", "App not updated", innerException));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ConstructorInnerException_WithBlankSubscriptionId_Fails(string subscriptionId)
        {
            var innerException = new Exception("The cause of the exception");
            Assert.Throws<ArgumentException>(
                () => new LogicAppNotUpdatedException(subscriptionId, "resource group", "logic app", "Trigger could not be found", innerException));
        }

        private static LogicAppNotUpdatedException SerializeDeserializeException(LogicAppNotUpdatedException exception)
        {
            var formatter = new BinaryFormatter();
            using (var contents = new MemoryStream())
            {
                formatter.Serialize(contents, exception);
                contents.Seek(0, 0);

                var deserialized = (LogicAppNotUpdatedException) formatter.Deserialize(contents);
                return deserialized;
            }
        }
    }
}
