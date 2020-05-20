using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Invictus.Testing.Model;
using Invictus.Testing.Serialization;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Invictus.Testing.Tests.Integration
{
    public class LogicAppsHelperTests : IDisposable
    {
        private readonly ITestOutputHelper _outputWriter;
        private readonly string _resourceGroup, _logicAppName, _logicAppMockingName;
        private readonly LogicAppsHelper _logicAppsHelper;

        private static readonly TestConfig Configuration = TestConfig.Create();

        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppsHelperTests"/> class.
        /// </summary>
        public LogicAppsHelperTests(ITestOutputHelper outputWriter)
        {
            _outputWriter = outputWriter;

            _resourceGroup = Configuration.GetAzureResourceGroup();
            _logicAppName = Configuration.GetTestLogicAppName();
            _logicAppMockingName = Configuration.GetTestMockingLogicAppName();

            string subscriptionId = Configuration.GetAzureSubscriptionId();
            string tenantId = Configuration.GetAzureTenantId();
            string clientId = Configuration.GetAzureClientId();
            string clientSecret = Configuration.GetAzureClientSecret();
            _logicAppsHelper = new LogicAppsHelper(subscriptionId, tenantId, clientId, clientSecret);
        }

        [Fact]
        public async Task GetLogicAppTriggerUrl_Success()
        {
            // Act
            LogicAppTriggerUrl logicAppTriggerUrl = await _logicAppsHelper.GetLogicAppTriggerUrlAsync(_resourceGroup, _logicAppName);

            // Assert
            Assert.NotNull(logicAppTriggerUrl.Value);
            Assert.Equal("POST", logicAppTriggerUrl.Method);
        }

        [Fact]
        public async Task GetLogicAppTriggerUrl_ByName_Success()
        {
            // Act
            LogicAppTriggerUrl logicAppTriggerUrl = 
                await _logicAppsHelper.GetLogicAppTriggerUrlAsync(_resourceGroup, _logicAppName, triggerName: "manual");

            // Assert
            Assert.NotNull(logicAppTriggerUrl.Value);
            Assert.Equal("POST", logicAppTriggerUrl.Method);
        }

        [Fact]
        public async Task PollForLogicAppRun_ByCorrelationId_Success()
        {
            // Arrange
            DateTime startTime = DateTime.UtcNow;
            
            string correlationId = $"correlationId-{Guid.NewGuid()}";
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId }
            };

            // Act
            LogicAppTriggerUrl logicAppTriggerUrl = await _logicAppsHelper.GetLogicAppTriggerUrlAsync(_resourceGroup, _logicAppName);

            // Assert
            Task<LogicAppRun> pollingTask = _logicAppsHelper.PollForLogicAppRunAsync(_resourceGroup, _logicAppName, startTime, correlationId);
            Task postTask = PostHeadersToLogicAppTriggerAsync(logicAppTriggerUrl.Value, headers);

            await Task.WhenAll(pollingTask, postTask);

            Assert.NotNull(pollingTask.Result);
            Assert.Equal(correlationId, pollingTask.Result.CorrelationId);
        }

        [Fact]
        public async Task PollForLogicAppRuns_ByCorrelationId_AfterTimeoutPeriod_Success()
        {
            // Arrange
            TimeSpan timeout = TimeSpan.FromSeconds(5);
            DateTime startTime = DateTime.UtcNow;

            string correlationId = $"correlationId-{Guid.NewGuid()}";
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId }
            };

            // Act
            LogicAppTriggerUrl logicAppTriggerUrl = await _logicAppsHelper.GetLogicAppTriggerUrlAsync(_resourceGroup, _logicAppName);

            // Assert
            // Poll for all logic app runs with provided correlation id after timeout period expires.
            Task<List<LogicAppRun>> pollingTask = 
                _logicAppsHelper.PollForLogicAppRunsAsync(_resourceGroup, _logicAppName, startTime, correlationId, timeout);
            
            // Run logic app twice with the same correlation id.
            Task postTask1 = PostHeadersToLogicAppTriggerAsync(logicAppTriggerUrl.Value, headers);
            Task postTask2 = PostHeadersToLogicAppTriggerAsync(logicAppTriggerUrl.Value, headers);

            await Task.WhenAll(pollingTask, postTask1, postTask2);

            Assert.NotNull(pollingTask.Result);
            Assert.Equal(2, pollingTask.Result.Count);
            foreach (var logicAppRun in pollingTask.Result)
            {
                Assert.Equal(correlationId, logicAppRun.CorrelationId);
            }
        }

        [Fact]
        //[Fact(Skip = "investigate in infinite running")]
        public async Task PollForLogicAppRuns_ByCorrelationId_NumberOfRuns_Success()
        {
            // Arrange
            const int numberOfRuns = 2;
            TimeSpan timeout = TimeSpan.FromSeconds(30);
            DateTime startTime = DateTime.UtcNow;

            string correlationId = $"correlationId-{Guid.NewGuid()}";
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId }
            };

            // Act
            Console.WriteLine("Get trigger URL of logic app");
            LogicAppTriggerUrl logicAppTriggerUrl = await _logicAppsHelper.GetLogicAppTriggerUrlAsync(_resourceGroup, _logicAppName);

            // Assert
            Console.WriteLine("Run logic app twice with same correlation id");
            // Run logic app twice with the same correlation id.
            Task postTask1 = PostHeadersToLogicAppTriggerAsync(logicAppTriggerUrl.Value, headers);
            Task postTask2 = PostHeadersToLogicAppTriggerAsync(logicAppTriggerUrl.Value, headers);
            await Task.WhenAll(postTask1, postTask2);

            Console.WriteLine("Poll for specific number of logic app runs with provided correlation id");
            // Poll for a specific number of logic app runs with provided correlation id.
            List<LogicAppRun> pollingTask = 
                await _logicAppsHelper.PollForLogicAppRunsAsync(_resourceGroup, _logicAppName, startTime, correlationId, timeout, numberOfRuns);

            Assert.Equal(numberOfRuns, pollingTask.Count);
            foreach (var logicAppRun in pollingTask)
            {
                Assert.Equal(correlationId, logicAppRun.CorrelationId);
            }
        }

        [Fact]
        public async Task PollForLogicAppRun_ByTrackedProperty_Success()
        {
            // Arrange
            const string trackedPropertyName = "trackedproperty";
            DateTime startTime = DateTime.UtcNow;

            string correlationId = $"correlationId-{Guid.NewGuid()}";
            string trackedPropertyValue = $"tracked-{Guid.NewGuid()}";

            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId },
                { "trackedpropertyheader1", trackedPropertyValue },
                { "trackedpropertyheader2", trackedPropertyValue }
            };

            // Act
            LogicAppTriggerUrl logicAppTriggerUrl = await _logicAppsHelper.GetLogicAppTriggerUrlAsync(_resourceGroup, _logicAppName);

            // Assert
            Task<LogicAppRun> pollingTask = 
                _logicAppsHelper.PollForLogicAppRunAsync(_resourceGroup, _logicAppName, startTime, trackedPropertyName, trackedPropertyValue);
            
            Task postTask = PostHeadersToLogicAppTriggerAsync(logicAppTriggerUrl.Value, headers);

            await Task.WhenAll(pollingTask, postTask);

            Assert.NotNull(pollingTask.Result);
            Assert.True(pollingTask.Result.TrackedProperties.ContainsValue(trackedPropertyValue));
        }

        [Fact]
        public async Task PollForLogicAppRun_ByTrackedProperty_DifferentValues_GetsLatest_Success()
        {
            // Arrange
            const string trackedPropertyName = "trackedproperty";
            DateTime startTime = DateTime.UtcNow;

            string correlationId = $"correlationId-{Guid.NewGuid()}";
            string trackedPropertyValue1 = $"tracked-{Guid.NewGuid()}";
            string trackedPropertyValue2 = $"tracked-{Guid.NewGuid()}";

            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId },
                { "trackedpropertyheader1", trackedPropertyValue1 },
                { "trackedpropertyheader2", trackedPropertyValue2 }
            };

            // Act
            LogicAppTriggerUrl logicAppTriggerUrl = await _logicAppsHelper.GetLogicAppTriggerUrlAsync(_resourceGroup, _logicAppName);

            // Assert
            Task<LogicAppRun> pollingTask = 
                _logicAppsHelper.PollForLogicAppRunAsync(_resourceGroup, _logicAppName, startTime, trackedPropertyName, trackedPropertyValue1);
            
            Task postTask = PostHeadersToLogicAppTriggerAsync(logicAppTriggerUrl.Value, headers);

            await Task.WhenAll(pollingTask, postTask);

            Assert.NotNull(pollingTask.Result);
            Assert.True(pollingTask.Result.TrackedProperties.ContainsValue(trackedPropertyValue2));
        }

        [Fact]
        public async Task PollForLogicAppRuns_ByTrackedProperty_AfterTimeoutPeriod_Success()
        {
            // Arrange
            const string trackedPropertyName = "trackedproperty";
            DateTime startTime = DateTime.UtcNow;
            TimeSpan timeout = TimeSpan.FromSeconds(5);
            
            string correlationId = $"correlationId-{Guid.NewGuid()}";
            string trackedPropertyValue = $"tracked-{Guid.NewGuid()}";

            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId },
                { "trackedpropertyheader1", trackedPropertyValue },
                { "trackedpropertyheader2", trackedPropertyValue }
            };

            // Act
            LogicAppTriggerUrl logicAppTriggerUrl = await _logicAppsHelper.GetLogicAppTriggerUrlAsync(_resourceGroup, _logicAppName);

            // Assert
            // Poll for all logic app runs with provided tracked property after timeout period expires.
            Task<List<LogicAppRun>> pollingTask = 
                _logicAppsHelper.PollForLogicAppRunsAsync(_resourceGroup, _logicAppName, startTime, trackedPropertyName, trackedPropertyValue, timeout);
            
            // Run logic app twice with the same tracked property.
            Task postTask1 = PostHeadersToLogicAppTriggerAsync(logicAppTriggerUrl.Value, headers);
            Task postTask2 = PostHeadersToLogicAppTriggerAsync(logicAppTriggerUrl.Value, headers);

            await Task.WhenAll(pollingTask, postTask1, postTask2);

            Assert.NotNull(pollingTask.Result);
            Assert.Equal(2, pollingTask.Result.Count);
            foreach (var logicAppRun in pollingTask.Result)
            {
                Assert.True(logicAppRun.TrackedProperties.ContainsValue(trackedPropertyValue));
            };
        }

        //[Fact]
        [Fact(Skip = "investigate in infinite running")]
        public async Task PollForLogicAppRuns_ByTrackedProperty_NumberOfRuns_Success()
        {
            // Arrange
            const string trackedPropertyName = "trackedproperty";
            const int numberOfRuns = 2;
            DateTime startTime = DateTime.UtcNow;
            TimeSpan timeout = TimeSpan.FromSeconds(30);
            
            string correlationId = $"correlationId-{Guid.NewGuid()}";
            var trackedPropertyValue = Guid.NewGuid().ToString();
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId },
                { "trackedpropertyheader1", trackedPropertyValue },
                { "trackedpropertyheader2", trackedPropertyValue }
            };

            // Act
            LogicAppTriggerUrl logicAppTriggerUrl = await _logicAppsHelper.GetLogicAppTriggerUrlAsync(_resourceGroup, _logicAppName);

            // Assert
            // Poll for a specific number of logic app runs with provided tracked property.
            Task<List<LogicAppRun>> pollingTask = 
                _logicAppsHelper.PollForLogicAppRunsAsync(_resourceGroup, _logicAppName, startTime, trackedPropertyName, trackedPropertyValue, timeout, numberOfRuns);

            // Run logic app twice with the same tracked property.
            Task postTask1 = PostHeadersToLogicAppTriggerAsync(logicAppTriggerUrl.Value, headers);
            Task postTask2 = PostHeadersToLogicAppTriggerAsync(logicAppTriggerUrl.Value, headers);

            await Task.WhenAll(pollingTask, postTask1, postTask2);

            Assert.NotNull(pollingTask.Result);
            Assert.Equal(numberOfRuns, pollingTask.Result.Count);
            foreach (var logicAppRun in pollingTask.Result)
            {
                Assert.True(logicAppRun.TrackedProperties.ContainsValue(trackedPropertyValue));
            };
        }

        [Fact]
        public async Task EnableStaticResultForAction_Success()
        {
            // Arrange
            const string actionName = "HTTP";
            
            string correlationId = $"correlationId-{Guid.NewGuid()}";
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId },
            };

            var staticResultDefinition = new StaticResultDefinition
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
            bool result = await _logicAppsHelper.EnableStaticResultForActionAsync(_resourceGroup, _logicAppMockingName, actionName, staticResultDefinition);
            
            // Assert
            Assert.True(result);

            await RunLogicAppOnTriggerUrlAsync(headers);
            LogicAppAction logicAppAction = await PollForLogicAppActionAsync(correlationId, actionName);
            
            Assert.Equal("200", logicAppAction.Outputs.statusCode.ToString());
            Assert.Equal("testvalue", logicAppAction.Outputs.headers["testheader"].ToString());
            Assert.True(logicAppAction.Outputs.body.ToString().Contains("test body"));
        }

        [Fact]
        public async Task EnableStaticResultForActions_Success()
        {
            // Arrange
            const string actionName = "HTTP";

            string correlationId = $"correlationId-{Guid.NewGuid()}";
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId },
            };

            var staticResultDefinition = new StaticResultDefinition
            {
                Outputs = new Outputs
                {
                    Headers = new Dictionary<string, string> { { "testheader", "testvalue" } },
                    StatusCode = "200",
                    Body = "test body"
                },
                Status = "Succeeded"
            };

            var actions = new Dictionary<string, StaticResultDefinition> { { actionName, staticResultDefinition } };

            // Act
            bool isSuccess = await _logicAppsHelper.EnableStaticResultForActionsAsync(_resourceGroup, _logicAppMockingName, actions);
            
            // Assert
            Assert.True(isSuccess);

            await RunLogicAppOnTriggerUrlAsync(headers);
            LogicAppAction logicAppAction = await PollForLogicAppActionAsync(correlationId, actionName);
            
            Assert.Equal("200", logicAppAction.Outputs.statusCode.ToString());
            Assert.Equal("testvalue", logicAppAction.Outputs.headers["testheader"].ToString());
            Assert.Equal("test body", logicAppAction.Outputs.body.ToString());
        }

        [Fact]
        public async Task DisableStaticResultForAction_Success()
        {
            // Arrange
            const string actionName = "HTTP";
            
            string correlationId = $"correlationId-{Guid.NewGuid()}";
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId },
            };

            // Act
            bool isSuccess = await _logicAppsHelper.DisableStaticResultForActionAsync(_resourceGroup, _logicAppMockingName, actionName);
            
            // Assert
            Assert.True(isSuccess);

            await RunLogicAppOnTriggerUrlAsync(headers);
            LogicAppAction logicAppAction = await PollForLogicAppActionAsync(correlationId, actionName);
            
            string body = logicAppAction.Outputs.body;
            Assert.NotEqual("test body", body);
        }

        [Fact]
        public async Task DisableStaticResultForAllActions_Success()
        {
            // Arrange
            DateTime startTime = DateTime.UtcNow.AddMinutes(-1);
            
            string correlationId = $"correlationId-{Guid.NewGuid()}";
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId },
            };

            // Act
            bool isSuccess = await _logicAppsHelper.DisableAllStaticResultsForLogicAppAsync(_resourceGroup, _logicAppMockingName);
            
            // Assert
            Assert.True(isSuccess);

            await RunLogicAppOnTriggerUrlAsync(headers);

            // Check logic app run for static result
            LogicAppRun logicAppRun = await _logicAppsHelper.PollForLogicAppRunAsync(_resourceGroup, _logicAppMockingName, startTime, correlationId);
            foreach (var action in logicAppRun.Actions)
            { 
                string body = action.Outputs.body;
                Assert.NotEqual("test body", body);
            };
        }

        private async Task RunLogicAppOnTriggerUrlAsync(IDictionary<string, string> headers)
        {
            LogicAppTriggerUrl logicAppTriggerUrl = await _logicAppsHelper.GetLogicAppTriggerUrlAsync(_resourceGroup, _logicAppMockingName);
            await PostHeadersToLogicAppTriggerAsync(logicAppTriggerUrl.Value, headers);
        }

        private async Task<LogicAppAction> PollForLogicAppActionAsync(string correlationId, string actionName)
        {
            DateTime startTime = DateTime.UtcNow.AddMinutes(-1);

            LogicAppRun logicAppRun = await _logicAppsHelper.PollForLogicAppRunAsync(_resourceGroup, _logicAppMockingName, startTime, correlationId);
            Assert.True(logicAppRun.Actions.Count != 0);
            
            LogicAppAction logicAppAction = logicAppRun.Actions.First(action => action.Name.Equals(actionName));
            Assert.NotNull(logicAppAction);

            return logicAppAction;
        }

        private static async Task PostHeadersToLogicAppTriggerAsync(string uri, IDictionary<string, string> headers)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, uri))
            {
                foreach ((string name, string value) in headers)
                {
                    request.Headers.Add(name, value);
                }

                using (var client = new HttpClient())
                using (HttpResponseMessage response = await client.SendAsync(request))
                {
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _logicAppsHelper?.Dispose();
        }
    }
}
