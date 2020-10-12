using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Invictus.Testing.LogicApps;
using Invictus.Testing.LogicApps.Model;
using Invictus.Testing.LogicApps.Serialization;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Invictus.Testing.Tests.Integration.LogicApps
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
                Assert.NotNull(logicAppTriggerUrl.Url);
                Assert.Equal("POST", logicAppTriggerUrl.Method); 
            }
        }

        [Fact]
        public async Task GetLogicAppTriggerUrl_ByName_Success()
        {
            // Arrange
            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppName, Authentication))
            {
                // Act
                LogicAppTriggerUrl logicAppTriggerUrl = await logicApp.GetTriggerUrlByNameAsync(triggerName: "manual");

                // Assert
                Assert.NotNull(logicAppTriggerUrl.Url);
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

            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppMockingName, Authentication, Logger))
            {
                await using (await logicApp.TemporaryEnableAsync())
                {
                    // Act
                    await using (await logicApp.TemporaryEnableSuccessStaticResultAsync(actionName))
                    {
                        // Assert
                        await logicApp.TriggerAsync(headers);
                        LogicAppAction enabledAction = await PollForLogicAppActionAsync(correlationId, actionName);

                        Assert.Equal(actionName, enabledAction.Name);
                        Assert.Equal("200", enabledAction.Outputs.statusCode.ToString());
                        Assert.Equal(LogicAppActionStatus.Succeeded, enabledAction.Status);
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

            var definitions = new Dictionary<string, StaticResultDefinition> { [actionName] = definition };

            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppMockingName, Authentication, Logger))
            {
                await using (await logicApp.TemporaryEnableAsync())
                {
                    // Act
                    await using (await logicApp.TemporaryEnableStaticResultsAsync(definitions))
                    {
                        // Assert
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
            // Arrange
            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppMockingName, Authentication, Logger))
            {
                // Act
                await using (await logicApp.TemporaryEnableAsync())
                {
                    // Assert
                    LogicAppMetadata metadata = await logicApp.GetMetadataAsync();
                    Assert.Equal(LogicAppState.Enabled, metadata.State);
                }
                {
                    LogicAppMetadata metadata = await logicApp.GetMetadataAsync();
                    Assert.Equal(LogicAppState.Disabled, metadata.State);
                }
            }
        }

        [Fact]
        public async Task TemporaryDisableLogicApp_Succeeds()
        {
            // Arrange
            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppMockingName, Authentication, Logger))
            {
                // Act
                await using (await logicApp.TemporaryDisableAsync())
                {
                    // Assert
                    LogicAppMetadata metadata = await logicApp.GetMetadataAsync();
                    Assert.Equal(LogicAppState.Disabled, metadata.State);
                }
                {
                    LogicAppMetadata metadata = await logicApp.GetMetadataAsync();
                    Assert.Equal(LogicAppState.Enabled, metadata.State);
                }
            }
        }

        [Fact]
        public async Task TemporaryUpdateLogicApp_WithUpdatedResponse_ReturnsUpdatedResponse()
        {
            // Arrange
            const string actionName = "Response";
            string correlationId = $"correlation-{Guid.NewGuid()}";
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId },
            };

            string definition = Configuration.GetLogicAppTriggerUpdateDefinition();

            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, TestBaseLogicAppName, Authentication, Logger))
            {
                await using (await logicApp.TemporaryEnableAsync())
                {
                    // Act
                    await using (await logicApp.TemporaryUpdateAsync(definition))
                    {
                        // Assert
                        await logicApp.TriggerAsync(headers);
                        LogicAppAction updatedAction = await PollForLogicAppActionAsync(correlationId, actionName, TestBaseLogicAppName);
                        Assert.Equal("Updated", updatedAction.Outputs.body.response.ToString());
                    }
                    {
                        await logicApp.TriggerAsync(headers);
                        LogicAppAction updatedAction = await PollForLogicAppActionAsync(correlationId, actionName, TestBaseLogicAppName);
                        Assert.Equal("Original", updatedAction.Outputs.body.response.ToString());
                    } 
                }
            }
        }

        [Fact]
        public async Task RunLogicApp_WithoutTrigger_ReturnsCorrelationId()
        {
            // Arrange
            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, TestBaseLogicAppName, Authentication, Logger))
            {
                await using (await logicApp.TemporaryEnableAsync())
                {
                    // Act
                    await logicApp.RunAsync();

                    // Assert
                    LogicAppRun run = await LogicAppsProvider
                        .LocatedAt(ResourceGroup, TestBaseLogicAppName, Authentication, Logger)
                        .WithStartTime(DateTimeOffset.UtcNow.AddMinutes(-1))
                        .PollForSingleLogicAppRunAsync();
                    
                    Assert.Contains("Response", run.Actions.Select(action => action.Name));
                }
            }
        }

        [Fact]
        public async Task RunByNameLogicApp_WithoutTrigger_ReturnsCorrelationId()
        {
            // Arrange
            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, TestBaseLogicAppName, Authentication, Logger))
            {
                await using (await logicApp.TemporaryEnableAsync())
                {
                    // Act
                    await logicApp.RunByNameAsync(triggerName: "manual");

                    // Assert
                    LogicAppRun run = await LogicAppsProvider
                        .LocatedAt(ResourceGroup, TestBaseLogicAppName, Authentication, Logger)
                        .WithStartTime(DateTimeOffset.UtcNow.AddMinutes(-1))
                        .PollForSingleLogicAppRunAsync();

                    Assert.Contains("Response", run.Actions.Select(action => action.Name));
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
            LogicAppAction action = await PollForLogicAppActionAsync(correlationId, actionName, LogicAppMockingName);
            return action;
        }

        private async Task<LogicAppAction> PollForLogicAppActionAsync(string correlationId, string actionName, string logicAppName)
        {
            LogicAppRun logicAppRun = await LogicAppsProvider
                .LocatedAt(ResourceGroup, logicAppName, Authentication, Logger)
                .WithStartTime(DateTimeOffset.UtcNow.AddMinutes(-1))
                .WithCorrelationId(correlationId)
                .PollForSingleLogicAppRunAsync();

            Assert.True(logicAppRun.Actions.Count() != 0);
            LogicAppAction logicAppAction1 = logicAppRun.Actions.First(action => action.Name.Equals(actionName));
            Assert.NotNull(logicAppAction1);
            LogicAppAction logicAppAction = logicAppAction1;
    
            return logicAppAction;
        }
    }
}
