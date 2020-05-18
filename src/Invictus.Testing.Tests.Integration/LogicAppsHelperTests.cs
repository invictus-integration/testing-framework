using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Invictus.Testing.Model;
using Invictus.Testing.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Invictus.Testing.Tests.Integration
{
    [TestClass]
    [TestCategory("Integration")]
    public class LogicAppsHelperTests
    {
        private readonly string resourceGroup = Configuration["Azure:ResourceGroup"];
        private readonly string logicAppName = Configuration["Azure:LogicApps:TestLogicAppName"];
        private readonly string logicAppMockingName = Configuration["Azure:LogicApps:TestMokingLogicAppName"];

        private static LogicAppsHelper _logicAppsHelper;

        private static readonly IConfiguration Configuration = 
            new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.local.json", optional: true)
                .Build();

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            var subscriptionId = Configuration["Azure:SubscriptionId"];
            var tenantId = Configuration["Azure:TenantId"];
            var clientId = Configuration["Azure:Authentication:ClientId"];
            var clientSecret = Configuration["Azure:Authentication:ClientSecret"];

            _logicAppsHelper = new LogicAppsHelper(subscriptionId, tenantId, clientId, clientSecret);
        }

        [TestMethod]
        public async Task GetLogicAppTriggerUrl_Success()
        {
            LogicAppTriggerUrl logicAppTriggerUrl = await _logicAppsHelper.GetLogicAppTriggerUrlAsync(resourceGroup, logicAppName);

            Assert.IsNotNull(logicAppTriggerUrl.Value);
            Assert.AreEqual("POST", logicAppTriggerUrl.Method);
        }

        [TestMethod]
        public async Task GetLogicAppTriggerUrl_ByName_Success()
        {
            LogicAppTriggerUrl logicAppTriggerUrl = await _logicAppsHelper.GetLogicAppTriggerUrlAsync(resourceGroup, logicAppName, "manual");

            Assert.IsNotNull(logicAppTriggerUrl.Value);
            Assert.AreEqual("POST", logicAppTriggerUrl.Method);
        }

        [TestMethod]
        public async Task PollForLogicAppRun_ByCorrelationId_Success()
        {
            var correlationId = Guid.NewGuid().ToString();
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId }
            };

            LogicAppTriggerUrl logicAppTriggerUrl = await _logicAppsHelper.GetLogicAppTriggerUrlAsync(resourceGroup, logicAppName);

            var pollingTask = _logicAppsHelper.PollForLogicAppRunAsync(resourceGroup, logicAppName, DateTime.UtcNow, correlationId);
            var postTask = Utils.PostAsync(logicAppTriggerUrl.Value, new StringContent(""), headers);

            await Task.WhenAll(pollingTask, postTask);

            Assert.IsNotNull(pollingTask.Result);
            Assert.AreEqual(correlationId, pollingTask.Result.CorrelationId);
        }

        [TestMethod]
        public async Task PollForLogicAppRuns_ByCorrelationId_AfterTimeoutPeriod_Success()
        {
            var correlationId = Guid.NewGuid().ToString();
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId }
            };

            LogicAppTriggerUrl logicAppTriggerUrl = await _logicAppsHelper.GetLogicAppTriggerUrlAsync(resourceGroup, logicAppName);

            // poll for all logic app runs with provided correlation id after timeout period expires
            var pollingTask = _logicAppsHelper.PollForLogicAppRunsAsync(resourceGroup, logicAppName, DateTime.UtcNow,
                correlationId, TimeSpan.FromSeconds(5));
            // run logic app twice with the same correlation id
            var postTask1 = Utils.PostAsync(logicAppTriggerUrl.Value, new StringContent(""), headers);
            var postTask2 = Utils.PostAsync(logicAppTriggerUrl.Value, new StringContent(""), headers);

            await Task.WhenAll(pollingTask, postTask1, postTask2);

            Assert.IsNotNull(pollingTask.Result);
            Assert.IsTrue(pollingTask.Result.Count == 2);
            foreach (var logicAppRun in pollingTask.Result)
            {
                Assert.AreEqual(correlationId, logicAppRun.CorrelationId);
            }
        }

        [TestMethod]
        public async Task PollForLogicAppRuns_ByCorrelationId_NumberOfRuns_Success()
        {
            var correlationId = Guid.NewGuid().ToString();
            var numberOfRuns = 2;
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId }
            };

            LogicAppTriggerUrl logicAppTriggerUrl = await _logicAppsHelper.GetLogicAppTriggerUrlAsync(resourceGroup, logicAppName);

            // poll for a specific number of logic app runs with provided correlation id
            var pollingTask = _logicAppsHelper.PollForLogicAppRunsAsync(resourceGroup, logicAppName, DateTime.UtcNow,
                correlationId, TimeSpan.FromSeconds(30), numberOfRuns);
            // run logic app twice with the same correlation id
            var postTask1 = Utils.PostAsync(logicAppTriggerUrl.Value, new StringContent(""), headers);
            var postTask2 = Utils.PostAsync(logicAppTriggerUrl.Value, new StringContent(""), headers);

            await Task.WhenAll(pollingTask, postTask1, postTask2);

            Assert.IsNotNull(pollingTask.Result);
            Assert.IsTrue(pollingTask.Result.Count == numberOfRuns);
            foreach (var logicAppRun in pollingTask.Result)
            {
                Assert.AreEqual(correlationId, logicAppRun.CorrelationId);
            }
        }

        [TestMethod]
        public async Task PollForLogicAppRun_ByTrackedProperty_Success()
        {
            var correlationId = Guid.NewGuid().ToString();
            var trackedpropertyValue = Guid.NewGuid().ToString();
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId },
                { "trackedpropertyheader1", trackedpropertyValue },
                { "trackedpropertyheader2", trackedpropertyValue }
            };

            LogicAppTriggerUrl logicAppTriggerUrl = await _logicAppsHelper.GetLogicAppTriggerUrlAsync(resourceGroup, logicAppName);

            var pollingTask = _logicAppsHelper.PollForLogicAppRunAsync(resourceGroup, logicAppName, DateTime.UtcNow,
                "trackedproperty", trackedpropertyValue);
            var postTask = Utils.PostAsync(logicAppTriggerUrl.Value, new StringContent(""), headers);

            await Task.WhenAll(pollingTask, postTask);

            Assert.IsNotNull(pollingTask.Result);
            Assert.IsTrue(pollingTask.Result.TrackedProperties.ContainsValue(trackedpropertyValue));
        }

        [TestMethod]
        public async Task PollForLogicAppRun_ByTrackedProperty_DifferentValues_GetsLatest_Success()
        {
            var correlationId = Guid.NewGuid().ToString();
            var trackedpropertyValue1 = Guid.NewGuid().ToString();
            var trackedpropertyValue2 = Guid.NewGuid().ToString();
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId },
                { "trackedpropertyheader1", trackedpropertyValue1 },
                { "trackedpropertyheader2", trackedpropertyValue2 }
            };

            LogicAppTriggerUrl logicAppTriggerUrl = await _logicAppsHelper.GetLogicAppTriggerUrlAsync(resourceGroup, logicAppName);

            var pollingTask = _logicAppsHelper.PollForLogicAppRunAsync(resourceGroup, logicAppName, DateTime.UtcNow,
                "trackedproperty", trackedpropertyValue1);
            var postTask = Utils.PostAsync(logicAppTriggerUrl.Value, new StringContent(""), headers);

            await Task.WhenAll(pollingTask, postTask);

            Assert.IsNotNull(pollingTask.Result);
            Assert.IsTrue(pollingTask.Result.TrackedProperties.ContainsValue(trackedpropertyValue2));
        }

        [TestMethod]
        public async Task PollForLogicAppRuns_ByTracketProperty_AfterTimeoutPeriod_Success()
        {
            var correlationId = Guid.NewGuid().ToString();
            var trackedpropertyValue = Guid.NewGuid().ToString();
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId },
                { "trackedpropertyheader1", trackedpropertyValue },
                { "trackedpropertyheader2", trackedpropertyValue }
            };

            LogicAppTriggerUrl logicAppTriggerUrl = await _logicAppsHelper.GetLogicAppTriggerUrlAsync(resourceGroup, logicAppName);

            // poll for all logic app runs with provided tracked property after timeout period expires
            var pollingTask = _logicAppsHelper.PollForLogicAppRunsAsync(resourceGroup, logicAppName, DateTime.UtcNow,
                "trackedproperty", trackedpropertyValue, TimeSpan.FromSeconds(5));
            // run logic app twice with the same tracked property
            var postTask1 = Utils.PostAsync(logicAppTriggerUrl.Value, new StringContent(""), headers);
            var postTask2 = Utils.PostAsync(logicAppTriggerUrl.Value, new StringContent(""), headers);

            await Task.WhenAll(pollingTask, postTask1, postTask2);

            Assert.IsNotNull(pollingTask.Result);
            Assert.IsTrue(pollingTask.Result.Count == 2);
            foreach (var logicAppRun in pollingTask.Result)
            {
                Assert.IsTrue(logicAppRun.TrackedProperties.ContainsValue(trackedpropertyValue));
            }
        }

        [TestMethod]
        public async Task PollForLogicAppRuns_ByTracketProperty_NumberOfRuns_Success()
        {
            var correlationId = Guid.NewGuid().ToString();
            var trackedpropertyValue = Guid.NewGuid().ToString();
            var numberOfRuns = 2;
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId },
                { "trackedpropertyheader1", trackedpropertyValue },
                { "trackedpropertyheader2", trackedpropertyValue }
            };

            LogicAppTriggerUrl logicAppTriggerUrl = await _logicAppsHelper.GetLogicAppTriggerUrlAsync(resourceGroup, logicAppName);

            // poll for a specific number of logic app runs with provided tracked property
            var pollingTask = _logicAppsHelper.PollForLogicAppRunsAsync(resourceGroup, logicAppName, DateTime.UtcNow,
                "trackedproperty", trackedpropertyValue, TimeSpan.FromSeconds(30), numberOfRuns);
            // run logic app twice with the same tracked property
            var postTask1 = Utils.PostAsync(logicAppTriggerUrl.Value, new StringContent(""), headers);
            var postTask2 = Utils.PostAsync(logicAppTriggerUrl.Value, new StringContent(""), headers);

            await Task.WhenAll(pollingTask, postTask1, postTask2);

            Assert.IsNotNull(pollingTask.Result);
            Assert.IsTrue(pollingTask.Result.Count == numberOfRuns);
            foreach (var logicAppRun in pollingTask.Result)
            {
                Assert.IsTrue(logicAppRun.TrackedProperties.ContainsValue(trackedpropertyValue));
            }
        }

        [TestMethod]
        public async Task EnableStaticResultForAction_Success()
        {
            var actionName = "HTTP";
            var correlationId = Guid.NewGuid().ToString();
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId },
            };

            StaticResultDefinition staticResultDefinition = new StaticResultDefinition
            {
                Outputs = new Outputs {
                    Headers = new Dictionary<string, string> { { "testheader", "testvalue" } },
                    StatusCode = "200",
                    Body = JToken.Parse("{id : 12345, name : 'test body'}")
                },
                Status = "Succeeded"
            };

            var result = await _logicAppsHelper.EnableStaticResultForActionAsync(resourceGroup, logicAppMockingName, actionName, staticResultDefinition);
            Assert.IsTrue(result);

            // run logic app
            var logicAppTriggerUrl = await _logicAppsHelper.GetLogicAppTriggerUrlAsync(resourceGroup, logicAppMockingName);
            await Utils.PostAsync(logicAppTriggerUrl.Value, new StringContent(""), headers);

            // check logic app run for static result
            var logicAppRun = await _logicAppsHelper.PollForLogicAppRunAsync(resourceGroup, logicAppMockingName, DateTime.UtcNow.AddMinutes(-1), correlationId);
            var logicAppAction = logicAppRun.Actions.FirstOrDefault(x => x.Name.Equals(actionName));

            Assert.AreEqual("200", logicAppAction.Outputs.statusCode.ToString());
            Assert.AreEqual("testvalue", logicAppAction.Outputs.headers["testheader"].ToString());
            Assert.IsTrue(logicAppAction.Outputs.body.ToString().Contains("test body"));
        }

        [TestMethod]
        public async Task EnableStaticResultForActions_Success()
        {
            var actionName = "HTTP";
            var correlationId = Guid.NewGuid().ToString();
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId },
            };

            StaticResultDefinition staticResultDefinition = new StaticResultDefinition
            {
                Outputs = new Outputs
                {
                    Headers = new Dictionary<string, string> { { "testheader", "testvalue" } },
                    StatusCode = "200",
                    Body = "test body"
                },
                Status = "Succeeded"
            };

            var result = await _logicAppsHelper.EnableStaticResultForActionsAsync(resourceGroup, logicAppMockingName, new Dictionary<string, StaticResultDefinition> { { actionName, staticResultDefinition } });
            Assert.IsTrue(result);

            // run logic app
            var logicAppTriggerUrl = await _logicAppsHelper.GetLogicAppTriggerUrlAsync(resourceGroup, logicAppMockingName);
            await Utils.PostAsync(logicAppTriggerUrl.Value, new StringContent(""), headers);

            // check logic app run for static result
            var logicAppRun = await _logicAppsHelper.PollForLogicAppRunAsync(resourceGroup, logicAppMockingName, DateTime.UtcNow.AddMinutes(-1), correlationId);
            var logicAppAction = logicAppRun.Actions.FirstOrDefault(x => x.Name.Equals(actionName));

            Assert.AreEqual("200", logicAppAction.Outputs.statusCode.ToString());
            Assert.AreEqual("testvalue", logicAppAction.Outputs.headers["testheader"].ToString());
            Assert.AreEqual("test body", logicAppAction.Outputs.body.ToString());
        }

        [TestMethod]
        public async Task DisableStaticResultForAction_Success()
        {
            var actionName = "HTTP";
            var correlationId = Guid.NewGuid().ToString();
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId },
            };

            var result = await _logicAppsHelper.DisableStaticResultForActionAsync(resourceGroup, logicAppMockingName, actionName);
            Assert.IsTrue(result);

            // run logic app
            var logicAppTriggerUrl = await _logicAppsHelper.GetLogicAppTriggerUrlAsync(resourceGroup, logicAppMockingName);
            await Utils.PostAsync(logicAppTriggerUrl.Value, new StringContent(""), headers);

            // check logic app run for static result
            var logicAppRun = await _logicAppsHelper.PollForLogicAppRunAsync(resourceGroup, logicAppMockingName, DateTime.UtcNow.AddMinutes(-1), correlationId);
            var logicAppAction = logicAppRun.Actions.FirstOrDefault(x => x.Name.Equals(actionName));

            Assert.AreNotEqual("test body", logicAppAction.Outputs.body);
        }

        [TestMethod]
        public async Task DisableStaticResultForAllActions_Success()
        {
            var correlationId = Guid.NewGuid().ToString();
            var headers = new Dictionary<string, string>
            {
                { "correlationId", correlationId },
            };

            var result = await _logicAppsHelper.DisableAllStaticResultsForLogicAppAsync(resourceGroup, logicAppMockingName);
            Assert.IsTrue(result);

            // run logic app
            var logicAppTriggerUrl = await _logicAppsHelper.GetLogicAppTriggerUrlAsync(resourceGroup, logicAppMockingName);
            await Utils.PostAsync(logicAppTriggerUrl.Value, new StringContent(""), headers);

            // check logic app run for static result
            var logicAppRun = await _logicAppsHelper.PollForLogicAppRunAsync(resourceGroup, logicAppMockingName, DateTime.UtcNow.AddMinutes(-1), correlationId);

            foreach (var action in logicAppRun.Actions)
            {
                Assert.AreNotEqual("test body", action.Outputs.body);
            }
        }
    }
}
