using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        /// Initializes a new instance of the <see cref="LogicAppsHelperTests"/> class.
        /// </summary>
        public LogicAppClientTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        [Fact]
        public async Task GetLogicAppTriggerUrl_Success()
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
                LogicAppTriggerUrl logicAppTriggerUrl = await logicApp.GetTriggerUrlAsync(triggerName: "manual");

                // Assert
                Assert.NotNull(logicAppTriggerUrl.Value);
                Assert.Equal("POST", logicAppTriggerUrl.Method); 
            }
        }

        [Fact]
        public async Task TemporaryEnableStaticResultForAction_Success()
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
                await using (await logicApp.TemporaryEnableStaticResultAsync(actionName, definition))
                {
                    // Act
                    await logicApp.TriggerAsync(headers);
                    LogicAppAction action = await PollForLogicAppActionAsync(correlationId, actionName);
            
                    Assert.Equal("200", action.Outputs.statusCode.ToString());
                    Assert.Equal("testvalue", action.Outputs.headers["testheader"].ToString());
                    Assert.True(action.Outputs.body.ToString().Contains("test body"));
                }
                {
                    await logicApp.TriggerAsync(headers);
                    LogicAppAction action = await PollForLogicAppActionAsync(correlationId, actionName);
            
                    string body = action.Outputs.body;
                    Assert.NotEqual("test body", body);
                }
                
            }
        }

        private async Task<LogicAppAction> PollForLogicAppActionAsync(string correlationId, string actionName)
        {
            LogicAppRun logicAppRun = await LogicAppsProvider
                .LocatedAt(ResourceGroup, LogicAppMockingName, Authentication, Logger)
                .WithStartTime(DateTime.UtcNow.AddMinutes(-1))
                .WithCorrelationId(correlationId)
                .PollForSingleLogicAppRunAsync();
            
            Assert.True(logicAppRun.Actions.Count() != 0);
            LogicAppAction logicAppAction = logicAppRun.Actions.First(action => action.Name.Equals(actionName));
            Assert.NotNull(logicAppAction);

            return logicAppAction;
        }
    }
}
