using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Invictus.Testing.Model;
using Invictus.Testing.Serialization;
using Microsoft.Azure.Management.Logic;
using Microsoft.Azure.Management.Logic.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure.OData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Retry;

namespace Invictus.Testing
{
    public class LogicAppsHelper : IDisposable
    {
        private readonly LogicManagementClient _logicManagementClient;
        private readonly string _resourceGroupPrefix;
        private readonly string _logicAppPrefix;

        private const int DefaultTimeoutInSeconds = 90;
        private const int PollIntervalInSeconds = 5;

        /// <summary>
        /// LogicAppsHelper constructor.
        /// </summary>
        /// <param name="subscriptionId">The Id of the Azure Subscription hosting the Logic Apps.</param>
        /// <param name="tenantId">The Directory Id of the Azure Subscription AD</param>
        /// <param name="clientId">The Object Id of the Service Principal with sufficient access to all required resource groups.</param>
        /// <param name="clientSecret">The corresponding key of the Service Principal</param>
        public LogicAppsHelper(string subscriptionId, string tenantId, string clientId, string clientSecret)
        {
            _logicManagementClient = GetLogicManagementClientAsync(subscriptionId, tenantId, clientId, clientSecret).Result;
        }

        /// <summary>
        /// LogicAppsHelper constructor.
        /// </summary>
        /// <param name="subscriptionId">The Id of the Azure Subscription hosting the Logic Apps.</param>
        /// <param name="tenantId">The Directory Id of the Azure Subscription AD</param>
        /// <param name="clientId">The Object Id of the Service Principal with sufficient access to all required resource groups.</param>
        /// <param name="clientSecret">The corresponding key of the Service Principal</param>
        /// <param name="resourceGroupPrefix">Prefix for Resource Group</param>
        /// <param name="logicAppPrefix">Prefix for Logic App name</param>
        public LogicAppsHelper(string subscriptionId, string tenantId, string clientId, string clientSecret, string resourceGroupPrefix = "", string logicAppPrefix = "")
        {
            _resourceGroupPrefix = resourceGroupPrefix;
            _logicAppPrefix = logicAppPrefix;

            _logicManagementClient = GetLogicManagementClientAsync(subscriptionId, tenantId, clientId, clientSecret).Result;
        }

        /// <summary>
        /// Enable a Logic App.
        /// </summary>
        /// <param name="resourceGroupName">The Azure Resource Group</param>
        /// <param name="logicAppName">The LogicApp name</param>
        /// <returns></returns>
        public async Task EnableLogicAppAsync(string resourceGroupName, string logicAppName)
        {
            resourceGroupName = PrefixResourceGroupName(resourceGroupName);
            logicAppName = PrefixLogicAppName(logicAppName);

            await _logicManagementClient.Workflows.EnableAsync(resourceGroupName, logicAppName);
        }

        /// <summary>
        /// Disable a Logic App.
        /// </summary>
        /// <param name="resourceGroupName">The Azure Resource Group</param>
        /// <param name="logicAppName">The LogicApp name</param>
        /// <returns></returns>
        public async Task DisableLogicAppAsync(string resourceGroupName, string logicAppName)
        {
            resourceGroupName = PrefixResourceGroupName(resourceGroupName);
            logicAppName = PrefixLogicAppName(logicAppName);

            await _logicManagementClient.Workflows.DisableAsync(resourceGroupName, logicAppName);
        }

        /// <summary>
        /// Delete a Logic App.
        /// </summary>
        /// <param name="resourceGroupName">The Azure Resource Group</param>
        /// <param name="logicAppName">The LogicApp name</param>
        /// <returns></returns>
        public async Task DeleteLogicAppAsync(string resourceGroupName, string logicAppName)
        {
            resourceGroupName = PrefixResourceGroupName(resourceGroupName);
            logicAppName = PrefixLogicAppName(logicAppName);

            await _logicManagementClient.Workflows.DeleteAsync(resourceGroupName, logicAppName);
        }

        /// <summary>
        /// Get Logic App.
        /// </summary>
        /// <param name="resourceGroupName">The Azure Resource Group</param>
        /// <param name="logicAppName">The LogicApp name</param>
        /// <returns>A LogicApp object</returns>
        public async Task<LogicApp> GetLogicAppAsync(string resourceGroupName, string logicAppName)
        {
            resourceGroupName = PrefixResourceGroupName(resourceGroupName);
            logicAppName = PrefixLogicAppName(logicAppName);

            var workflow = await _logicManagementClient.Workflows.GetAsync(resourceGroupName, logicAppName);
            return (LogicApp)workflow;
        }

        /// <summary>
        /// Update Logic App definition.
        /// </summary>
        /// <param name="resourceGroupName">The Azure Resource Group</param>
        /// <param name="logicAppName">The LogicApp name</param>
        /// <param name="logicAppDefinition">The LogicApp definition in JSON format</param>
        /// <returns>True is operation succeeded, false otherwise</returns>
        public async Task<bool> UpdateLogicAppAsync(string resourceGroupName, string logicAppName, string logicAppDefinition)
        {
            resourceGroupName = PrefixResourceGroupName(resourceGroupName);
            logicAppName = PrefixLogicAppName(logicAppName);
            var success = false;
            var workflow = await _logicManagementClient.Workflows.GetAsync(resourceGroupName, logicAppName);

            workflow.Definition = JObject.Parse(logicAppDefinition);
            var resultWorkflow = await
                _logicManagementClient.Workflows.CreateOrUpdateAsync(resourceGroupName, logicAppName, workflow);

            if (resultWorkflow.Name == logicAppName)
            {
                success = true;
            }

            return success;
        }

        /// <summary>
        /// Enable default static result for Logic App action.
        /// </summary>
        /// <param name="resourceGroupName">The Azure Resource Group</param>
        /// <param name="logicAppName">The LogicApp name</param>
        /// <param name="actionName">The LogicApp action name</param>
        /// <returns>True is operation succeeded, false otherwise</returns>
        public async Task<bool> EnableStaticResultForActionAsync(string resourceGroupName, string logicAppName, string actionName)
        {
            resourceGroupName = PrefixResourceGroupName(resourceGroupName);
            logicAppName = PrefixLogicAppName(logicAppName);
            var success = false;

            var workflow = await _logicManagementClient.Workflows.GetAsync(resourceGroupName, logicAppName);
            LogicAppDefinition logicAppDefinition = JsonConvert.DeserializeObject<LogicAppDefinition>(workflow.Definition.ToString());

            ActionDefinition actionDefinition = logicAppDefinition.Actions[actionName];
            if (actionDefinition == null) return false;

            if (actionDefinition.RuntimeConfiguration != null)
            {
                actionDefinition.RuntimeConfiguration.StaticResult.StaticResultOptions = "Enabled";
            }
            else
            {
                var staticResultName = $"{actionName}0";
                actionDefinition.RuntimeConfiguration = new RuntimeConfiguration
                {
                    StaticResult = new StaticResult { Name = staticResultName, StaticResultOptions = "Enabled" }
                };

                StaticResultDefinition staticResultDefinition = new StaticResultDefinition
                {
                    Outputs = new Outputs { Headers = new Dictionary<string, string>(), StatusCode = "OK" },
                    Status = "Succeeded"
                };
                if (logicAppDefinition.StaticResults == null)
                    logicAppDefinition.StaticResults = new Dictionary<string, StaticResultDefinition>();

                logicAppDefinition.StaticResults.Add(staticResultName, staticResultDefinition);
            }

            workflow.Definition = JObject.Parse(JsonConvert.SerializeObject(logicAppDefinition));

            var resultWorkflow = await _logicManagementClient.Workflows.CreateOrUpdateAsync(resourceGroupName, logicAppName, workflow);
            if (resultWorkflow.Name == logicAppName)
            {
                success = true;
            }

            return success;
        }

        /// <summary>
        /// Enable static result for Logic App action.
        /// </summary>
        /// <param name="resourceGroupName">The Azure Resource Group</param>
        /// <param name="logicAppName">The LogicApp name</param>
        /// <param name="actionName">The LogicApp action name</param>
        /// <param name="staticResultDefinition">The Static Result definition</param>
        /// <returns>True is operation succeeded, false otherwise</returns>
        public async Task<bool> EnableStaticResultForActionAsync(string resourceGroupName, string logicAppName, string actionName, StaticResultDefinition staticResultDefinition)
        {
            resourceGroupName = PrefixResourceGroupName(resourceGroupName);
            logicAppName = PrefixLogicAppName(logicAppName);
            var success = false;

            var workflow = await _logicManagementClient.Workflows.GetAsync(resourceGroupName, logicAppName);
            LogicAppDefinition logicAppDefinition = JsonConvert.DeserializeObject<LogicAppDefinition>(workflow.Definition.ToString());

            ActionDefinition actionDefinition = logicAppDefinition.Actions[actionName];
            if (actionDefinition == null) return false;

            var staticResultName = $"{actionName}0";
            if (actionDefinition.RuntimeConfiguration != null)
            {
                actionDefinition.RuntimeConfiguration.StaticResult.StaticResultOptions = "Enabled";
            }
            else
            {
                actionDefinition.RuntimeConfiguration = new RuntimeConfiguration
                {
                    StaticResult = new StaticResult { Name = staticResultName, StaticResultOptions = "Enabled" }
                };
            }

            if (logicAppDefinition.StaticResults == null)
                logicAppDefinition.StaticResults = new Dictionary<string, StaticResultDefinition>();

            if (logicAppDefinition.StaticResults.ContainsKey(staticResultName))
                logicAppDefinition.StaticResults[staticResultName] = staticResultDefinition;
            else
                logicAppDefinition.StaticResults.Add(staticResultName, staticResultDefinition);

            workflow.Definition = JObject.Parse(JsonConvert.SerializeObject(logicAppDefinition));

            var resultWorkflow = await _logicManagementClient.Workflows.CreateOrUpdateAsync(resourceGroupName, logicAppName, workflow);
            if (resultWorkflow.Name == logicAppName)
            {
                success = true;
            }

            return success;
        }

        /// <summary>
        /// Enable static result for Logic App actions.
        /// </summary>
        /// <param name="resourceGroupName">The Azure Resource Group</param>
        /// <param name="logicAppName">The LogicApp name</param>
        /// <param name="actions">List of LogicApp actions and static result definitions</param>
        /// <returns>True is operation succeeded, false otherwise</returns>
        public async Task<bool> EnableStaticResultForActionsAsync(string resourceGroupName, string logicAppName, Dictionary<string, StaticResultDefinition> actions)
        {
            resourceGroupName = PrefixResourceGroupName(resourceGroupName);
            logicAppName = PrefixLogicAppName(logicAppName);
            var success = false;

            var workflow = await _logicManagementClient.Workflows.GetAsync(resourceGroupName, logicAppName);
            LogicAppDefinition logicAppDefinition = JsonConvert.DeserializeObject<LogicAppDefinition>(workflow.Definition.ToString());

            foreach (var item in actions)
            {
                var actionName = item.Key;
                var staticResultDefinition = item.Value;
                ActionDefinition actionDefinition = logicAppDefinition.Actions[actionName];
                if (actionDefinition == null) continue;

                var staticResultName = $"{actionName}0";
                if (actionDefinition.RuntimeConfiguration != null)
                {
                    actionDefinition.RuntimeConfiguration.StaticResult.StaticResultOptions = "Enabled";
                }
                else
                {
                    actionDefinition.RuntimeConfiguration = new RuntimeConfiguration
                    {
                        StaticResult = new StaticResult { Name = staticResultName, StaticResultOptions = "Enabled" }
                    };
                }

                if (logicAppDefinition.StaticResults == null)
                    logicAppDefinition.StaticResults = new Dictionary<string, StaticResultDefinition>();

                if (logicAppDefinition.StaticResults.ContainsKey(staticResultName))
                    logicAppDefinition.StaticResults[staticResultName] = staticResultDefinition;
                else
                    logicAppDefinition.StaticResults.Add(staticResultName, staticResultDefinition);
            }


            workflow.Definition = JObject.Parse(JsonConvert.SerializeObject(logicAppDefinition));
            var resultWorkflow = await _logicManagementClient.Workflows.CreateOrUpdateAsync(resourceGroupName, logicAppName, workflow);
            if (resultWorkflow.Name == logicAppName)
            {
                success = true;
            }

            return success;
        }

        /// <summary>
        /// Disable static result for Logic App action.
        /// </summary>
        /// <param name="resourceGroupName">The Azure Resource Group</param>
        /// <param name="logicAppName">The LogicApp name</param>
        /// <param name="actionName">The LogicApp action name</param>
        /// <returns>True is operation succeeded, false otherwise</returns>
        public async Task<bool> DisableStaticResultForActionAsync(string resourceGroupName, string logicAppName, string actionName)
        {
            resourceGroupName = PrefixResourceGroupName(resourceGroupName);
            logicAppName = PrefixLogicAppName(logicAppName);
            var success = false;

            var workflow = await _logicManagementClient.Workflows.GetAsync(resourceGroupName, logicAppName);
            LogicAppDefinition logicAppDefinition = JsonConvert.DeserializeObject<LogicAppDefinition>(workflow.Definition.ToString());

            ActionDefinition actionDefinition = logicAppDefinition.Actions[actionName];
            if (actionDefinition == null) return false;

            if (actionDefinition.RuntimeConfiguration != null)
            {
                actionDefinition.RuntimeConfiguration.StaticResult.StaticResultOptions = "Disabled";
            }

            workflow.Definition = JObject.Parse(JsonConvert.SerializeObject(logicAppDefinition));

            var resultWorkflow = await _logicManagementClient.Workflows.CreateOrUpdateAsync(resourceGroupName, logicAppName, workflow);
            if (resultWorkflow.Name == logicAppName)
            {
                success = true;
            }

            return success;
        }

        /// <summary>
        /// Disables static results for all actions of a Logic App.
        /// </summary>
        /// <param name="resourceGroupName">The Azure Resource Group</param>
        /// <param name="logicAppName">The LogicApp name</param>
        /// <returns></returns>
        public async Task<bool> DisableAllStaticResultsForLogicAppAsync(string resourceGroupName, string logicAppName)
        {
            resourceGroupName = PrefixResourceGroupName(resourceGroupName);
            logicAppName = PrefixLogicAppName(logicAppName);
            var success = false;

            var workflow = await _logicManagementClient.Workflows.GetAsync(resourceGroupName, logicAppName);
            LogicAppDefinition logicAppDefinition = JsonConvert.DeserializeObject<LogicAppDefinition>(workflow.Definition.ToString());

            foreach (var item in logicAppDefinition.Actions)
            {
                ActionDefinition actionDefinition = item.Value;

                if (actionDefinition.RuntimeConfiguration != null)
                {
                    actionDefinition.RuntimeConfiguration.StaticResult.StaticResultOptions = "Disabled";
                }

            }

            workflow.Definition = JObject.Parse(JsonConvert.SerializeObject(logicAppDefinition));

            var resultWorkflow = await _logicManagementClient.Workflows.CreateOrUpdateAsync(resourceGroupName, logicAppName, workflow);
            if (resultWorkflow.Name == logicAppName)
            {
                success = true;
            }

            return success;
        }

        /// <summary>
        /// Get Logic App Trigger Url
        /// </summary>
        /// <param name="resourceGroupName">The Azure Resource Group</param>
        /// <param name="logicAppName">The LogicApp name</param>
        /// <param name="triggerName">The LogicApp trigger name</param>
        /// <returns>A LogicAppTriggerUrl object</returns>
        public async Task<LogicAppTriggerUrl> GetLogicAppTriggerUrlAsync(string resourceGroupName, string logicAppName, string triggerName = "")
        {
            resourceGroupName = PrefixResourceGroupName(resourceGroupName);
            logicAppName = PrefixLogicAppName(logicAppName);
            if (string.IsNullOrEmpty(triggerName))
            {
                triggerName = await GetTriggerName(resourceGroupName, logicAppName);
            }
            var callbackUrl = await _logicManagementClient.WorkflowTriggers.ListCallbackUrlAsync(resourceGroupName, logicAppName, triggerName);

            return new LogicAppTriggerUrl { Value = callbackUrl.Value, Method = callbackUrl.Method };
        }

        /// <summary>
        /// Runs a Logic App.
        /// </summary>
        /// <param name="resourceGroupName">The Azure Resource Group</param>
        /// <param name="logicAppName">The LogicApp name</param>
        /// <param name="triggerName">The LogicApp trigger name</param>
        /// <returns></returns>
        public async Task RunLogicAppAsync(string resourceGroupName, string logicAppName, string triggerName = "")
        {
            resourceGroupName = PrefixResourceGroupName(resourceGroupName);
            logicAppName = PrefixLogicAppName(logicAppName);
            if (string.IsNullOrEmpty(triggerName))
            {
                triggerName = await GetTriggerName(resourceGroupName, logicAppName);
            }

            await _logicManagementClient.WorkflowTriggers.RunAsync(resourceGroupName, logicAppName, triggerName);
        }

        /// <summary>
        /// Get Logic App Run By Id.
        /// </summary>
        /// <param name="resourceGroupName">The Azure Resource Group</param>
        /// <param name="logicAppName">The LogicApp name</param>
        /// <param name="Id">The LogicApp run identifier</param>
        /// <returns>A LogicApp run</returns>
        public async Task<LogicAppRun> GetLogicAppRunAsync(string resourceGroupName, string logicAppName, string Id)
        {
            resourceGroupName = PrefixResourceGroupName(resourceGroupName);
            logicAppName = PrefixLogicAppName(logicAppName);

            var workflowRun = await _logicManagementClient.WorkflowRuns.GetAsync(resourceGroupName, logicAppName, Id);
            return Converter.ToLogicAppRun(this, resourceGroupName, logicAppName, workflowRun);
        }

        /// <summary>
        /// Poll for logic app run by correlation id.
        /// </summary>
        /// <param name="resourceGroupName">The Azure Resource Group</param>
        /// <param name="logicAppName">The LogicApp name</param>
        /// <param name="startTime">The start time of the LogicApp run</param>
        /// <param name="correlationId">The correlation Id</param>
        /// <param name="timeout">The timeout period in seconds</param>
        /// <returns>A LogicApp run</returns>
        public async Task<LogicAppRun> PollForLogicAppRunAsync(string resourceGroupName, string logicAppName, DateTime startTime, string correlationId,
            TimeSpan? timeout = null)
        {
            if (timeout == null) { timeout = TimeSpan.FromSeconds(DefaultTimeoutInSeconds); }

            resourceGroupName = PrefixResourceGroupName(resourceGroupName);
            logicAppName = PrefixLogicAppName(logicAppName);

            var odataQuery = GetOdataQuery(startTime, correlationId);

            return await Poll(
                async () =>
                {
                    var result = await _logicManagementClient.WorkflowRuns.ListAsync(resourceGroupName, logicAppName, odataQuery);
                    return result.Select(x => Converter.ToLogicAppRun(this, resourceGroupName, logicAppName, x)).FirstOrDefault();
                },
                PollIntervalInSeconds,
                timeout.Value);
        }

        /// <summary>
        /// Poll for logic app runs after timeout expired.
        /// </summary>
        /// <param name="resourceGroupName">The Azure Resource Group</param>
        /// <param name="logicAppName">The LogicApp name</param>
        /// <param name="startTime">The start time of the LogicApp run</param>
        /// <param name="correlationId">The correlation Id</param>
        /// <param name="timeout">The timeout period in seconds</param>
        /// <param name="numberOfItems">Expected number of items</param>
        /// <returns>List of LogicApp runs</returns>
        public async Task<List<LogicAppRun>> PollForLogicAppRunsAsync(string resourceGroupName, string logicAppName, DateTime startTime, string correlationId,
            TimeSpan? timeout = null, int numberOfItems = 0)
        {
            if (timeout == null) { timeout = TimeSpan.FromSeconds(DefaultTimeoutInSeconds); }

            resourceGroupName = PrefixResourceGroupName(resourceGroupName);
            logicAppName = PrefixLogicAppName(logicAppName);

            if (numberOfItems > 0)
            {
                return await Poll(
                    async () => await FindLogicAppRunsByCorrelationIdAsync(resourceGroupName, logicAppName, startTime, correlationId),
                    numberOfItems,
                    PollIntervalInSeconds,
                    timeout.Value);
            }
            else
            {
                return await PollAfterTimeout(
                    async () => await FindLogicAppRunsByCorrelationIdAsync(resourceGroupName, logicAppName, startTime, correlationId),
                    timeout.Value);
            }
        }

        /// <summary>
        /// Poll for logic app run by tracked property.
        /// </summary>
        /// <param name="resourceGroupName">The Azure Resource Group</param>
        /// <param name="logicAppName">The LogicApp name</param>
        /// <param name="startTime">The start time of the LogicApp run</param>
        /// <param name="trackedPropertyName">Tracked Property name</param>
        /// <param name="trackedProperyValue">Tracked Property value</param>
        /// <param name="timeout">The timeout period in seconds</param>
        /// <returns>A LogicApp run</returns>
        public async Task<LogicAppRun> PollForLogicAppRunAsync(string resourceGroupName, string logicAppName, DateTime startTime,
          string trackedPropertyName, string trackedProperyValue, TimeSpan? timeout = null)
        {
            if (timeout == null) { timeout = TimeSpan.FromSeconds(DefaultTimeoutInSeconds); }

            resourceGroupName = PrefixResourceGroupName(resourceGroupName);
            logicAppName = PrefixLogicAppName(logicAppName);

            return await Poll(
                async () => await FindLogicAppRunByTrackedPropertyAsync(resourceGroupName, logicAppName, startTime, trackedPropertyName, trackedProperyValue),
                PollIntervalInSeconds,
                timeout.Value
                );
        }

        /// <summary>
        /// Poll for logic app runs after timeout expired.
        /// </summary>
        /// <param name="resourceGroupName">The Azure Resource Group</param>
        /// <param name="logicAppName">The LogicApp name</param>
        /// <param name="startTime">The start time of the LogicApp run</param>
        /// <param name="trackedPropertyName">Tracked Property name</param>
        /// <param name="trackedProperyValue">Tracked Property value</param>
        /// <param name="timeout">The timeout period in seconds</param>
        /// <param name="numberOfItems">Expected number of items</param>
        /// <returns>List of LogicApp runs</returns>
        public async Task<List<LogicAppRun>> PollForLogicAppRunsAsync(string resourceGroupName, string logicAppName, DateTime startTime,
          string trackedPropertyName, string trackedProperyValue, TimeSpan? timeout = null, int numberOfItems = 0)
        {
            if (timeout == null) { timeout = TimeSpan.FromSeconds(DefaultTimeoutInSeconds); }

            resourceGroupName = PrefixResourceGroupName(resourceGroupName);
            logicAppName = PrefixLogicAppName(logicAppName);

            if (numberOfItems > 0)
            {
                return await Poll(
                    async () => await FindLogicAppRunsByTrackedPropertyAsync(resourceGroupName, logicAppName, startTime, trackedPropertyName, trackedProperyValue),
                    numberOfItems,
                    PollIntervalInSeconds,
                    timeout.Value);
            }
            else
            {
                return await PollAfterTimeout(
                async () => await FindLogicAppRunsByTrackedPropertyAsync(resourceGroupName, logicAppName, startTime, trackedPropertyName, trackedProperyValue),
                timeout.Value);
            }
        }

        /// <summary>
        /// Get LogicApp Run actions.
        /// </summary>
        /// <param name="resourceGroupName">The Azure Resource Group</param>
        /// <param name="logicAppName">The LogicApp name</param>
        /// <param name="runName">The LogicApp Run Identifier</param>
        /// <param name="usePrefix"></param>
        /// <returns>List of LogicApp actions</returns>
        public async Task<List<LogicAppAction>> GetLogicAppRunActionsAsync(string resourceGroupName, string logicAppName, string runName, bool usePrefix = true)
        {
            if (usePrefix)
            {
                resourceGroupName = PrefixResourceGroupName(resourceGroupName);
                logicAppName = PrefixLogicAppName(logicAppName);
            }

            return await FindLogicAppRunActionsAsync(resourceGroupName, logicAppName, runName);
        }

        #region Private Methods
        private async Task<LogicManagementClient> GetLogicManagementClientAsync(string subscriptionId, string tenantId, string clientId, string clientSecret)
        {
            var authority = string.Format("{0}{1}", "https://login.windows.net/", tenantId);

            var authContext = new AuthenticationContext(authority);
            var credential = new ClientCredential(clientId, clientSecret);

            var token = await authContext.AcquireTokenAsync("https://management.azure.com/", credential);

            return new LogicManagementClient(new TokenCredentials(token.AccessToken)) { SubscriptionId = subscriptionId };
        }

        private async Task<LogicAppRun> FindLogicAppRunByTrackedPropertyAsync(string resourceGroupName, string logicAppName, DateTime startTime, string trackedPropertyName, string trackedProperyValue)
        {
            LogicAppRun result = null;

            var odataQuery = new ODataQuery<WorkflowRunFilter>
            {
                Filter = $"StartTime ge {startTime.ToString("O")} and Status ne 'Running'"
            };
            var workFlowRuns = await _logicManagementClient.WorkflowRuns.ListAsync(resourceGroupName, logicAppName, odataQuery);

            foreach (var workFlowRun in workFlowRuns)
            {
                var actions = await FindLogicAppRunActionsAsync(resourceGroupName, logicAppName, workFlowRun.Name);

                bool trackedPropertyFound = actions
                    .Where(a => a.TrackedProperties != null)
                    .Any(a => a.TrackedProperties.Count(t => t.Key.Equals(trackedPropertyName, StringComparison.OrdinalIgnoreCase) && t.Value.Equals(trackedProperyValue, StringComparison.OrdinalIgnoreCase)) > 0);

                if (trackedPropertyFound)
                {
                    result = Converter.ToLogicAppRun(workFlowRun, actions);
                    break;
                }
            }

            return result;
        }

        private async Task<List<LogicAppRun>> FindLogicAppRunsByTrackedPropertyAsync(string resourceGroupName, string logicAppName, DateTime startTime, string trackedPropertyName, string trackedProperyValue)
        {
            var result = new List<LogicAppRun>();

            var odataQuery = new ODataQuery<WorkflowRunFilter>
            {
                Filter = $"StartTime ge {startTime.ToString("O")} and Status ne 'Running'"
            };
            var workFlowRuns = await _logicManagementClient.WorkflowRuns.ListAsync(resourceGroupName, logicAppName, odataQuery);

            Parallel.ForEach(workFlowRuns, (workFlowRun) =>
            {
                var actions = FindLogicAppRunActionsAsync(resourceGroupName, logicAppName, workFlowRun.Name).Result;

                bool trackedPropertyFound = actions
                    .Where(a => a.TrackedProperties != null)
                    .Any(a => a.TrackedProperties.Count(t => t.Key.Equals(trackedPropertyName, StringComparison.OrdinalIgnoreCase) && t.Value.Equals(trackedProperyValue, StringComparison.OrdinalIgnoreCase)) > 0);

                if (trackedPropertyFound)
                {
                    var logicAppRun = Converter.ToLogicAppRun(workFlowRun, actions);
                    result.Add(logicAppRun);
                }

            });

            return result;
        }

        private async Task<List<LogicAppRun>> FindLogicAppRunsByCorrelationIdAsync(string resourceGroupName, string logicAppName, DateTime startTime, string correlationId)
        {
            var odataQuery = GetOdataQuery(startTime, correlationId);

            var result = await _logicManagementClient.WorkflowRuns.ListAsync(resourceGroupName, logicAppName, odataQuery);
            return result.Select(x => Converter.ToLogicAppRun(this, resourceGroupName, logicAppName, x)).ToList();
        }

        private async Task<List<LogicAppAction>> FindLogicAppRunActionsAsync(string resourceGroupName, string logicAppName, string runName)
        {
            var actions = new List<LogicAppAction>();

            var workflowRunActions = await _logicManagementClient.WorkflowRunActions.ListAsync(resourceGroupName, logicAppName, runName);
            foreach (var workflowRunAction in workflowRunActions)
            {
                actions.Add(await Converter.ToLogicAppActionAsync(workflowRunAction));
            }

            return actions;
        }

        private async Task<string> GetTriggerName(string resourceGroupName, string logicAppName)
        {
            var triggerName = string.Empty;
            var triggers = await _logicManagementClient.WorkflowTriggers.ListAsync(resourceGroupName, logicAppName);
            if (triggers.Count() > 0)
            {
                triggerName = triggers.First().Name;
            }

            return triggerName;
        }

        private string PrefixResourceGroupName(string resourceGroupName)
        {
            return $"{_resourceGroupPrefix}{resourceGroupName}";
        }

        private string PrefixLogicAppName(string logicAppName)
        {
            return $"{_logicAppPrefix}{logicAppName}";
        }

        private ODataQuery<WorkflowRunFilter> GetOdataQuery(DateTime startTime, string correlationId)
        {
            return new ODataQuery<WorkflowRunFilter>
            {
                Filter = $"StartTime ge {startTime.ToString("O")} and ClientTrackingId eq '{correlationId}' and Status ne 'Running'"
            };
        }

        private async Task<T> Poll<T>(Func<Task<T>> condition, int pollIntervalSeconds, TimeSpan timeout)
        {
            RetryPolicy<T> retryPolicy = 
                Policy.HandleResult<T>(result => result == null)
                      .WaitAndRetryForeverAsync(index => TimeSpan.FromSeconds(pollIntervalSeconds));

            return await Policy.TimeoutAsync(timeout)
                               .WrapAsync(retryPolicy)
                               .ExecuteAsync(condition);
        }

        private async Task<List<T>> Poll<T>(Func<Task<List<T>>> condition, int count, int pollIntervalSeconds, TimeSpan timeout)
        {
            RetryPolicy<List<T>> retryPolicy = 
                Policy.HandleResult<List<T>>(results => results.Count < count)
                      .WaitAndRetryForeverAsync(index => TimeSpan.FromSeconds(pollIntervalSeconds));
            
            return await Policy.TimeoutAsync(timeout)
                               .WrapAsync(retryPolicy)
                               .ExecuteAsync(condition);
        }

        private async Task<T> PollAfterTimeout<T>(Func<Task<T>> returnDelegate, TimeSpan timeout)
        {
            await Task.Delay(timeout);
            return await returnDelegate();
        }
        #endregion

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _logicManagementClient?.Dispose();
        }
    }
}
