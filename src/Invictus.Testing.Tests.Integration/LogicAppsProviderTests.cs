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
        public async Task PollForLogicAppRun_WithoutLogger_Success()
        {
            // Arrange
            string correlationId = $"correlationId-{Guid.NewGuid()}";
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId }
            };

            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppName, Authentication, Logger))
            await using (await logicApp.TemporaryEnableAsync())
            {
                Task postTask = logicApp.TriggerAsync(headers);

                // Act
                Task<IEnumerable<LogicAppRun>> pollingTask =
                    LogicAppsProvider.LocatedAt(ResourceGroup, LogicAppName, Authentication)
                                     .WithCorrelationId(correlationId)
                                     .PollForLogicAppRunsAsync();

                await Task.WhenAll(pollingTask, postTask);

                // Assert
                Assert.NotNull(pollingTask.Result);
                LogicAppRun logicAppRun = Assert.Single(pollingTask.Result);
                Assert.NotNull(logicAppRun);
                Assert.Equal(correlationId, logicAppRun.CorrelationId);
            }
        }

        [Fact]
        public async Task PollForLogicAppRun_NotMatchedCorrelation_Fails()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "correlationId", $"correlationId-{Guid.NewGuid()}" }
            };

            // Act
            Task<LogicAppRun> pollingTask =
                LogicAppsProvider.LocatedAt(ResourceGroup, LogicAppName, Authentication, Logger)
                                 .WithTimeout(TimeSpan.FromSeconds(5))
                                 .WithCorrelationId("not-matched-correlation-ID")
                                 .PollForSingleLogicAppRunAsync();

            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppName, Authentication, Logger))
            {
                // Assert
                await logicApp.TriggerAsync(headers);
                await Assert.ThrowsAsync<TimeoutException>(() => pollingTask);
            }
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

            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppName, Authentication, Logger))
            await using (await logicApp.TemporaryEnableAsync())
            {
                Task postTask = logicApp.TriggerAsync(headers);

                // Act
                Task<LogicAppRun> pollingTask =
                    LogicAppsProvider.LocatedAt(ResourceGroup, LogicAppName, Authentication, Logger)
                                     .WithCorrelationId(correlationId)
                                     .PollForSingleLogicAppRunAsync();

                await Task.WhenAll(pollingTask, postTask);

                // Assert
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

            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppName, Authentication))
            await using (await logicApp.TemporaryEnableAsync())
            {
                // Run logic app twice with the same correlation id.
                Task postTask1 = logicApp.TriggerAsync(headers);
                Task postTask2 = logicApp.TriggerAsync(headers);

                // Act
                Task<IEnumerable<LogicAppRun>> pollingTask =
                    LogicAppsProvider.LocatedAt(ResourceGroup, LogicAppName, Authentication, Logger)
                                     .WithCorrelationId(correlationId)
                                     .WithTimeout(timeout)
                                     .PollForLogicAppRunsAsync(numberOfRuns);

                await Task.WhenAll(pollingTask, postTask1, postTask2);

                // Assert
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

            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppName, Authentication))
            await using (await logicApp.TemporaryEnableAsync())
            {
                Task postTask = logicApp.TriggerAsync(headers);
             
                // Act
                Task<LogicAppRun> pollingTask =
                    LogicAppsProvider.LocatedAt(ResourceGroup, LogicAppName, Authentication, Logger)
                                     .WithTrackedProperty(trackedPropertyName, trackedPropertyValue)
                                     .PollForSingleLogicAppRunAsync();

                await Task.WhenAll(pollingTask, postTask);

                // Assert
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

            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppName, Authentication))
            await using (await logicApp.TemporaryEnableAsync())
            {
                Task postTask = logicApp.TriggerAsync(headers);

                // Act
                Task<LogicAppRun> pollingTask =
                    LogicAppsProvider.LocatedAt(ResourceGroup, LogicAppName, Authentication, Logger)
                                     .WithTrackedProperty(trackedPropertyName, trackedPropertyValue1)
                                     .PollForSingleLogicAppRunAsync();

                await Task.WhenAll(pollingTask, postTask);

                // Assert
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

            using (var logicApp = await LogicAppClient.CreateAsync(ResourceGroup, LogicAppName, Authentication))
            await using (await logicApp.TemporaryEnableAsync())
            {
                // Run logic app twice with the same tracked property.
                Task postTask1 = logicApp.TriggerAsync(headers);
                Task postTask2 = logicApp.TriggerAsync(headers);

                // Act
                // Poll for a specific number of logic app runs with provided tracked property.
                Task<IEnumerable<LogicAppRun>> pollingTask =
                    LogicAppsProvider.LocatedAt(ResourceGroup, LogicAppName, Authentication, Logger)
                                     .WithTrackedProperty(trackedPropertyName, trackedPropertyValue)
                                     .WithTimeout(timeout)
                                     .PollForLogicAppRunsAsync(numberOfRuns);

                await Task.WhenAll(pollingTask, postTask1, postTask2);

                // Assert
                Assert.NotNull(pollingTask.Result);
                Assert.Equal(numberOfRuns, pollingTask.Result.Count());
                Assert.All(pollingTask.Result, logicAppRun =>
                {
                    Assert.Contains(logicAppRun.TrackedProperties, property => property.Value == trackedPropertyValue);
                }); 
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Constructor_WithBlankResourceGroup_Fails(string resourceGroup)
        {
            Assert.Throws<ArgumentException>(
                () => LogicAppsProvider.LocatedAt(resourceGroup, LogicAppName, Authentication));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Constructor_WithBlankLogicApp_Fails(string logicApp)
        {
            Assert.Throws<ArgumentException>(
                () => LogicAppsProvider.LocatedAt(ResourceGroup, logicApp, Authentication));
        }

        [Fact]
        public void Constructor_WithoutAuthentication_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => LogicAppsProvider.LocatedAt(ResourceGroup, LogicAppName, authentication: null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ConstructorWithLogger_WithBlankResourceGroup_Fails(string resourceGroup)
        {
            Assert.Throws<ArgumentException>(
                () => LogicAppsProvider.LocatedAt(resourceGroup, LogicAppName, Authentication, Logger));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ConstructorWithLogger_WithBlankLogicApp_Fails(string logicApp)
        {
            Assert.Throws<ArgumentException>(
                () => LogicAppsProvider.LocatedAt(ResourceGroup, logicApp, Authentication, Logger));
        }

        [Fact]
        public void ConstructorWithLogger_WithoutAuthentication_Fails()
        {
            Assert.ThrowsAny<ArgumentException>(
                () => LogicAppsProvider.LocatedAt(ResourceGroup, LogicAppName, authentication: null, logger: Logger));
        }
    }
}
