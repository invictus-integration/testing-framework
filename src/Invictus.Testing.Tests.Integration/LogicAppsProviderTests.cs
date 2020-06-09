using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Invictus.Testing.Model;
using Xunit;
using Xunit.Abstractions;

namespace Invictus.Testing.Tests.Integration
{
    public class LogicAppsProviderTests : IntegrationTest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppsProviderTests"/> class.
        /// </summary>
        public LogicAppsProviderTests(ITestOutputHelper outputWriter) : base(outputWriter)
        {
        }

        [Fact]
        public async Task PollForLogicAppRun_ByCorrelationId_Success()
        {
            // Arrange
            string correlationId = $"correlationId-{Guid.NewGuid()}";
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId }
            };

            // Act
            Task<LogicAppRun> pollingTask =
                LogicAppsProvider.LocatedAt(ResourceGroup, LogicAppName, Authentication, Logger)
                                 .WithCorrelationId(correlationId)
                                 .PollForSingleLogicAppRunAsync();


            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppName, Authentication))
            {
                // Assert
                Task postTask = logicApp.TriggerAsync(headers);
                await Task.WhenAll(pollingTask, postTask);

                Assert.NotNull(pollingTask.Result);
                Assert.Equal(correlationId, pollingTask.Result.CorrelationId);
            }
        }

        [Fact]
        public async Task PollForLogicAppRuns_ByCorrelationId_NumberOfRuns_Success()
        {
            // Arrange
            const int numberOfRuns = 2;
            TimeSpan timeout = TimeSpan.FromSeconds(30);

            string correlationId = $"correlationId-{Guid.NewGuid()}";
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId }
            };

            // Act
            // Poll for a specific number of logic app runs with provided correlation id.
            Task<IEnumerable<LogicAppRun>> pollingTask =
                LogicAppsProvider.LocatedAt(ResourceGroup, LogicAppName, Authentication, Logger)
                                 .WithCorrelationId(correlationId)
                                 .WithTimeout(timeout)
                                 .PollForLogicAppRunsAsync(numberOfRuns);
            
            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppName, Authentication))
            {
                // Run logic app twice with the same correlation id.
                Task postTask1 = logicApp.TriggerAsync(headers);
                Task postTask2 = logicApp.TriggerAsync(headers);
                await Task.WhenAll(pollingTask, postTask1, postTask2);

                Assert.Equal(numberOfRuns, pollingTask.Result.Count());
                Assert.All(pollingTask.Result, logicAppRun =>
                {
                    Assert.Equal(correlationId, logicAppRun.CorrelationId);
                }); 
            }
        }

        [Fact]
        public async Task PollForLogicAppRun_ByTrackedProperty_Success()
        {
            // Arrange
            const string trackedPropertyName = "trackedproperty";
            string correlationId = $"correlationId-{Guid.NewGuid()}";
            string trackedPropertyValue = $"tracked-{Guid.NewGuid()}";

            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId },
                { "trackedpropertyheader1", trackedPropertyValue },
                { "trackedpropertyheader2", trackedPropertyValue }
            };

            // Act
            Task<LogicAppRun> pollingTask =
                LogicAppsProvider.LocatedAt(ResourceGroup, LogicAppName, Authentication, Logger)
                                 .WithTrackedProperty(trackedPropertyName, trackedPropertyValue)
                                 .PollForSingleLogicAppRunAsync();

            // Assert
            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppName, Authentication))
            {
                Task postTask = logicApp.TriggerAsync(headers);
                await Task.WhenAll(pollingTask, postTask);

                Assert.NotNull(pollingTask.Result);
                Assert.Contains(pollingTask.Result.TrackedProperties, property => property.Value == trackedPropertyValue);
            }
        }

        [Fact]
        public async Task PollForLogicAppRun_ByTrackedProperty_DifferentValues_GetsLatest_Success()
        {
            // Arrange
            const string trackedPropertyName = "trackedproperty";
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
            Task<LogicAppRun> pollingTask =
                LogicAppsProvider.LocatedAt(ResourceGroup, LogicAppName, Authentication, Logger)
                                 .WithTrackedProperty(trackedPropertyName, trackedPropertyValue1)
                                 .PollForSingleLogicAppRunAsync();

            // Assert
            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppName, Authentication))
            {
                Task postTask = logicApp.TriggerAsync(headers);
                await Task.WhenAll(pollingTask, postTask);

                Assert.NotNull(pollingTask.Result);
                Assert.Contains(pollingTask.Result.TrackedProperties, property => property.Value == trackedPropertyValue2); 
            }
        }

        [Fact]
        public async Task PollForLogicAppRuns_ByTrackedProperty_NumberOfRuns_Success()
        {
            // Arrange
            const string trackedPropertyName = "trackedproperty";
            const int numberOfRuns = 2;
            TimeSpan timeout = TimeSpan.FromSeconds(40);
            
            string correlationId = $"correlationId-{Guid.NewGuid()}";
            var trackedPropertyValue = Guid.NewGuid().ToString();
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId },
                { "trackedpropertyheader1", trackedPropertyValue },
                { "trackedpropertyheader2", trackedPropertyValue }
            };

            // Act
            // Poll for a specific number of logic app runs with provided tracked property.
            Task<IEnumerable<LogicAppRun>> pollingTask = 
                LogicAppsProvider.LocatedAt(ResourceGroup, LogicAppName, Authentication, Logger)
                                 .WithTrackedProperty(trackedPropertyName, trackedPropertyValue)
                                 .WithTimeout(timeout)
                                 .PollForLogicAppRunsAsync(numberOfRuns);

            // Assert
            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppName, Authentication))
            {
                // Run logic app twice with the same tracked property.
                Task postTask1 = logicApp.TriggerAsync(headers);
                Task postTask2 = logicApp.TriggerAsync(headers);
                await Task.WhenAll(pollingTask, postTask1, postTask2);

                Assert.NotNull(pollingTask.Result);
                Assert.Equal(numberOfRuns, pollingTask.Result.Count());
                Assert.All(pollingTask.Result, logicAppRun =>
                {
                    Assert.Contains(logicAppRun.TrackedProperties, property => property.Value == trackedPropertyValue);
                }); 
            }
        }
    }
}
