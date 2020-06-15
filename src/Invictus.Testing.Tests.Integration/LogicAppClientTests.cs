using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Invictus.Testing.Model;
using Invictus.Testing.Serialization;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Invictus.Testing.Tests.Integration
{
    public class LogicAppClientTests : IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppClientTests"/> class.
        /// </summary>
        public LogicAppClientTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        [Fact]
        public async Task GetLogicAppTriggerUrl_NoTriggerNameSpecified_Success()
        {
            // Arrange
            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppName, Authentication))
            {
                // Act
                LogicAppTriggerUrl logicAppTriggerUrl = await logicApp.GetTriggerUrlAsync();

                // Assert
                Assert.NotNull(logicAppTriggerUrl.Value);
                Assert.Equal("POST", logicAppTriggerUrl.Method); 
            }
        }

        [Fact]
        public async Task GetLogicAppTriggerUrl_ByName_Success()
        {
            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppName, Authentication))
            {
                // Act
                LogicAppTriggerUrl logicAppTriggerUrl = await logicApp.GetTriggerUrlByNameAsync(triggerName: "manual");

                // Assert
                Assert.NotNull(logicAppTriggerUrl.Value);
                Assert.Equal("POST", logicAppTriggerUrl.Method); 
            }
        }

        [Fact]
        public async Task TemporaryEnableSuccessStaticResultForAction_WithoutConsumerStaticResult_Success()
        {
            // Arrange
            const string actionName = "HTTP";
            string correlationId = $"correlationId-{Guid.NewGuid()}";
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId },
            };

            // Act
            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppMockingName, Authentication, Logger))
            {
                await using (await logicApp.TemporaryEnableAsync())
                {
                    await using (await logicApp.TemporaryEnableSuccessStaticResultAsync(actionName))
                    {
                        // Act
                        await logicApp.TriggerAsync(headers);
                        LogicAppAction enabledAction = await PollForLogicAppActionAsync(correlationId, actionName);

                        Assert.Equal(actionName, enabledAction.Name);
                        Assert.Equal("200", enabledAction.Outputs.statusCode.ToString());
                        Assert.Equal("Succeeded", enabledAction.Status);
                    }

                    await logicApp.TriggerAsync(headers);
                    LogicAppAction disabledAction = await PollForLogicAppActionAsync(correlationId, actionName);

                    Assert.NotEmpty(disabledAction.Outputs.headers);
                }
            }
        }

        [Fact]
        public async Task TemporaryEnableStaticResultsForAction_WithSuccessStaticResult_Success()
        {
            // Arrange
            const string actionName = "HTTP";
            string correlationId = $"correlationId-{Guid.NewGuid()}";
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId },
            };

            var definition = new StaticResultDefinition
            {
                Outputs = new Outputs
                {
                    Headers = new Dictionary<string, string> { { "testheader", "testvalue" } },
                    StatusCode = "200",
                    Body = JToken.Parse("{id : 12345, name : 'test body'}")
                },
                Status = "Succeeded"
            };

            // Act
            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppMockingName, Authentication, Logger))
            {
                await using (await logicApp.TemporaryEnableAsync())
                {
                    var definitions = new Dictionary<string, StaticResultDefinition> { [actionName] = definition };
                    await using (await logicApp.TemporaryEnableStaticResultsAsync(definitions))
                    {
                        // Act
                        await logicApp.TriggerAsync(headers);
                        LogicAppAction enabledAction = await PollForLogicAppActionAsync(correlationId, actionName);

                        Assert.Equal("200", enabledAction.Outputs.statusCode.ToString());
                        Assert.Equal("testvalue", enabledAction.Outputs.headers["testheader"].ToString());
                        Assert.Contains("test body", enabledAction.Outputs.body.ToString());
                    }

                    await logicApp.TriggerAsync(headers);
                    LogicAppAction disabledAction = await PollForLogicAppActionAsync(correlationId, actionName);

                    Assert.DoesNotContain("test body", disabledAction.Outputs.body.ToString());
                }
            }
        }

        [Fact]
        public async Task TemporaryEnableStaticResultForAction_WithSuccessStaticResult_Success()
        {
            // Arrange
            const string actionName = "HTTP";
            string correlationId = $"correlationId-{Guid.NewGuid()}";
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId },
            };

            var definition = new StaticResultDefinition
            {
                Outputs = new Outputs 
                {
                    Headers = new Dictionary<string, string> { { "testheader", "testvalue" } },
                    StatusCode = "200",
                    Body = JToken.Parse("{id : 12345, name : 'test body'}")
                },
                Status = "Succeeded"
            };

            // Act
            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppMockingName, Authentication))
            {
                await using (await logicApp.TemporaryEnableAsync())
                {
                    await using (await logicApp.TemporaryEnableStaticResultAsync(actionName, definition))
                    {
                        // Act
                        await logicApp.TriggerAsync(headers);
                        LogicAppAction enabledAction = await PollForLogicAppActionAsync(correlationId, actionName);

                        Assert.Equal("200", enabledAction.Outputs.statusCode.ToString());
                        Assert.Equal("testvalue", enabledAction.Outputs.headers["testheader"].ToString());
                        Assert.Contains("test body", enabledAction.Outputs.body.ToString());
                    }

                    await logicApp.TriggerAsync(headers);
                    LogicAppAction disabledAction = await PollForLogicAppActionAsync(correlationId, actionName);

                    Assert.DoesNotContain("test body", disabledAction.Outputs.body.ToString());
                }
            }
        }

        [Fact]
        public async Task TemporaryEnableLogicApp_Success()
        {
            // Act
            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppMockingName, Authentication, Logger))
            {
                await using (await logicApp.TemporaryEnableAsync())
                {
                    // Assert
                    LogicApp metadata = await logicApp.GetMetadataAsync();
                    Assert.Equal("Enabled", metadata.State);
                }
                {
                    LogicApp metadata = await logicApp.GetMetadataAsync();
                    Assert.Equal("Disabled", metadata.State);
                }
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task Constructor_WithBlankResourceGroup_Fails(string resourceGroup)
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => LogicAppClient.CreateAsync(resourceGroup, LogicAppName, Authentication));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task Constructor_WithBlankLogicApp_Fails(string logicApp)
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => LogicAppClient.CreateAsync(ResourceGroup, logicApp, Authentication));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task ConstructorWithLogger_WithBlankResourceGroup_Fails(string resourceGroup)
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => LogicAppClient.CreateAsync(resourceGroup, LogicAppName, Authentication, Logger));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task ConstructorWithLogger_WithBlankLogicApp_Fails(string logicApp)
        {
            await Assert.ThrowsAsync<ArgumentException>(
                () => LogicAppClient.CreateAsync(ResourceGroup, logicApp, Authentication, Logger));
        }

        [Fact]
        public async Task Constructor_WithNullAuthentication_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => LogicAppClient.CreateAsync(ResourceGroup, LogicAppName, authentication: null));
        }

        [Fact]
        public async Task ConstructorWithLogger_WithNullAuthentication_Fails()
        {
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => LogicAppClient.CreateAsync(ResourceGroup, LogicAppName, authentication: null, logger: Logger));
        }

        private async Task<LogicAppAction> PollForLogicAppActionAsync(string correlationId, string actionName)
        {
            LogicAppRun logicAppRun = await LogicAppsProvider
                .LocatedAt(ResourceGroup, LogicAppMockingName, Authentication, Logger)
                .WithStartTime(DateTimeOffset.UtcNow.AddMinutes(-1))
                .WithCorrelationId(correlationId)
                .PollForSingleLogicAppRunAsync();
            
            Assert.True(logicAppRun.Actions.Count() != 0);
            LogicAppAction logicAppAction = logicAppRun.Actions.First(action => action.Name.Equals(actionName));
            Assert.NotNull(logicAppAction);

            return logicAppAction;
        }
    }
}
