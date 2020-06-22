using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GuardNet;
using Invictus.Testing.LogicApps.Model;
using Invictus.Testing.LogicApps.Serialization;
using Microsoft.Azure.Management.Logic;
using Microsoft.Azure.Management.Logic.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Rest.Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Invictus.Testing.LogicApps 
{
    /// <summary>
    /// Representing client operations on a given logic app running in Azure.
    /// </summary>
    public class LogicAppClient : IDisposable
    {
        private readonly string _resourceGroup, _logicAppName;
        private readonly LogicManagementClient _logicManagementClient;
        private readonly ILogger _logger;

        private static readonly HttpClient HttpClient = new HttpClient();

        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppClient"/> class.
        /// </summary>
        /// <param name="resourceGroup">The resource group where the logic app is located.</param>
        /// <param name="logicAppName">The name of the logic app resource running in Azure.</param>
        /// <param name="client">The logic management client to run REST operations during client operations.</param>
        /// <param name="logger">The instance to write diagnostic trace messages while interacting with the logic app.</param>
        public LogicAppClient(string resourceGroup, string logicAppName, LogicManagementClient client, ILogger logger)
        {
            Guard.NotNullOrEmpty(resourceGroup, nameof(resourceGroup));
            Guard.NotNullOrEmpty(logicAppName, nameof(logicAppName));
            Guard.NotNull(client, nameof(client));

            _resourceGroup = resourceGroup;
            _logicAppName = logicAppName;
            _logicManagementClient = client;
            _logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppClient"/> class.
        /// </summary>
        /// <param name="resourceGroup">The resource group where the logic app is located.</param>
        /// <param name="logicAppName">The name of the logic app resource running in Azure.</param>
        /// <param name="client">The logic management client to run REST operations during client operations.</param>
        public LogicAppClient(string resourceGroup, string logicAppName, LogicManagementClient client)
            : this(resourceGroup, logicAppName, client, NullLogger.Instance)
        {
        }

        /// <summary>
        /// Creates a new authenticated instance of the <see cref="LogicAppClient"/>.
        /// </summary>
        /// <param name="resourceGroup">The resource group where the logic app is located.</param>
        /// <param name="logicAppName">The name of the logic app resource running in Azure.</param>
        /// <param name="authentication">The authentication mechanism to authenticate this client.</param>
        /// <returns>
        ///     An authenticated client capable of interacting with the logic app resource running in Azure.
        /// </returns>
        public static async Task<LogicAppClient> CreateAsync(
            string resourceGroup,
            string logicAppName,
            LogicAuthentication authentication)
        {
            Guard.NotNullOrEmpty(resourceGroup, nameof(resourceGroup));
            Guard.NotNullOrEmpty(logicAppName, nameof(logicAppName));
            Guard.NotNull(authentication, nameof(authentication));

            return await CreateAsync(resourceGroup, logicAppName, authentication, NullLogger.Instance);
        }

        /// <summary>
        /// Creates a new authenticated instance of the <see cref="LogicAppClient"/>.
        /// </summary>
        /// <param name="resourceGroup">The resource group where the logic app is located.</param>
        /// <param name="logicAppName">The name of the logic app resource running in Azure.</param>
        /// <param name="authentication">The authentication mechanism to authenticate this client.</param>
        /// <param name="logger">The instance to write diagnostic trace messages while interacting with the logic app.</param>
        /// <returns>
        ///     An authenticated client capable of interacting with the logic app resource running in Azure.
        /// </returns>
        public static async Task<LogicAppClient> CreateAsync(
            string resourceGroup,
            string logicAppName,
            LogicAuthentication authentication,
            ILogger logger)
        {
            Guard.NotNullOrEmpty(resourceGroup, nameof(resourceGroup));
            Guard.NotNullOrEmpty(logicAppName, nameof(logicAppName));
            Guard.NotNull(authentication, nameof(authentication));

            LogicManagementClient managementClient = await authentication.AuthenticateAsync();
            logger = logger ?? NullLogger.Instance;
            return new LogicAppClient(resourceGroup, logicAppName, managementClient, logger);
        }

        /// <summary>
        /// Temporary enables the current logic app resource on Azure, and disables the logic app after the returned instance gets disposed.
        /// </summary>
        /// <returns>
        ///     An instance to control the removal of the updates.
        /// </returns>
        public async Task<IAsyncDisposable> TemporaryEnableAsync()
        {
            return await AsyncDisposable.CreateAsync(
                async () =>
                {
                    _logger.LogTrace("Enables (+) the workflow on logic app '{LogicAppName}' in resource group '{ResourceGroup}'", _logicAppName, _resourceGroup);
                    await _logicManagementClient.Workflows.EnableAsync(_resourceGroup, _logicAppName);
                },
                async () =>
                {
                    _logger.LogTrace("Disables (-) the workflow on logic app '{LogicAppName}' in resource group '{ResourceGroup}'", _logicAppName, _resourceGroup);
                    await _logicManagementClient.Workflows.DisableAsync(_resourceGroup, _logicAppName);
                });
        }

        /// <summary>
        /// Updates the current JSON logic app definition with the given <paramref name="logicAppDefinition"/>,
        /// and removes this update after the returned instance gets disposed.
        /// </summary>
        /// <param name="logicAppDefinition">Then JSON representation of the new definition.</param>
        /// <returns>
        ///     An instance to control the removal of the updates.
        /// </returns>
        public async Task<IAsyncDisposable> TemporaryUpdateAsync(string logicAppDefinition)
        {
            Guard.NotNull(logicAppDefinition, nameof(logicAppDefinition));

            Workflow workflow = await _logicManagementClient.Workflows.GetAsync(_resourceGroup, _logicAppName);
            object originalAppDefinition = workflow.Definition;

            _logger.LogTrace("Updates (+) the logic app '{LogicAppName}' workflow definition in resource group '{ResourceGroup}'", _logicAppName, _resourceGroup);
            await UpdateAsync(workflow, JObject.Parse(logicAppDefinition));
            return AsyncDisposable.Create(async () =>
            {
                _logger.LogTrace("Reverts (-) the update of the logic app '{LogicAppName}' workflow definition in resource group '{ResourceGroup}'", _logicAppName, _resourceGroup);
                await UpdateAsync(workflow, originalAppDefinition);
            });
        }

        private async Task UpdateAsync(Workflow workflow, object logicAppDefinition)
        {
            workflow.Definition = logicAppDefinition;
            Workflow resultWorkflow = 
                await _logicManagementClient.Workflows.CreateOrUpdateAsync(_resourceGroup, _logicAppName, workflow);

            if (resultWorkflow.Name != _logicAppName)
            {
                throw new InvalidOperationException(
                    $"Could not update the logic app '{_logicAppName}' workflow in resource group '{_resourceGroup}'correctly, "
                    + "please make sure the authentication on this resource is set correctly and has the right access to this resource");
            }
        }

        /// <summary>
        /// Deletes the current logic app resource on Azure.
        /// </summary>
        public async Task DeleteAsync()
        {
            _logger.LogTrace("Deletes the workflow of logic app '{LogicAppName}' in resource group '{ResourceGroup}'", _logicAppName, _resourceGroup);
            await _logicManagementClient.Workflows.DeleteAsync(_resourceGroup, _logicAppName);
        }

        /// <summary>
        /// Runs the current logic app resource by searching for triggers on the logic app.
        /// </summary>
        /// <exception cref="LogicAppTriggerNotFoundException">When no trigger can be found on the logic app.</exception>
        public async Task<object> RunAsync()
        {
            string triggerName = await GetTriggerNameAsync();
            object result = await RunByNameAsync(triggerName);

            return result;
        }

        /// <summary>
        /// Runs the current logic app resource using the given <paramref name="triggerName"/>.
        /// </summary>
        /// <param name="triggerName">The name of the trigger that executes a workflow in the logic app.</param>
        public async Task<object> RunByNameAsync(string triggerName)
        {
            Guard.NotNullOrEmpty(triggerName, nameof(triggerName));

            _logger.LogTrace("Run the workflow trigger of logic app '{LogicAppName}' in resource group '{ResourceGroup}'", _logicAppName, _resourceGroup);
            
            object result = await _logicManagementClient.WorkflowTriggers.RunAsync(_resourceGroup, _logicAppName, triggerName);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="runName"></param>
        /// <returns></returns>
        public async Task CancelAsync(string runName)
        {
            Guard.NotNullOrEmpty(runName, nameof(runName));

            _logger.LogTrace("Cancel the workflow run of logic app '{LogicAppName}' in resource group '{ResourceGroup}'", _logicAppName, _resourceGroup);
            await _logicManagementClient.WorkflowRuns.CancelAsync(_resourceGroup, _logicAppName, runName);
        }
        
        /// <summary>
        /// Gets the logic app definition information.
        /// </summary>
        public async Task<LogicApp> GetMetadataAsync()
        {
            Workflow workflow = await _logicManagementClient.Workflows.GetAsync(_resourceGroup, _logicAppName);
            return LogicAppConverter.ToLogicApp(workflow);
        }

        /// <summary>
        /// Run logic app on the current trigger URL, posting the given <paramref name="headers"/>.
        /// </summary>
        /// <param name="headers">The headers to send with the trigger URL of the current logic app.</param>
        public async Task TriggerAsync(IDictionary<string, string> headers)
        {
            Guard.NotNull(headers, nameof(headers));
            Guard.NotAny(headers, nameof(headers));

            LogicAppTriggerUrl triggerUrl = await GetTriggerUrlAsync();

            _logger.LogTrace("Trigger the workflow of logic app '{LogicAppName}' in resource group '{ResourceGroup}'", _logicAppName, _resourceGroup);
            using (var request = new HttpRequestMessage(HttpMethod.Post, triggerUrl.Url))
            {
                foreach (KeyValuePair<string, string> header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }

                using (HttpResponseMessage response = await HttpClient.SendAsync(request))
                {
                }
            }
        }

        /// <summary>
        /// Gets the URL on which the workflow with trigger can be run by searching the workflow for configured triggers.
        /// </summary>
        /// <exception cref="LogicAppTriggerNotFoundException">When no trigger can be found on the logic app.</exception>
        public async Task<LogicAppTriggerUrl> GetTriggerUrlAsync()
        {
            string triggerName = await GetTriggerNameAsync();
            LogicAppTriggerUrl triggerUrl = await GetTriggerUrlByNameAsync(triggerName);
            
            return triggerUrl;
        }

        /// <summary>
        /// Gets the URL on which the workflow with the <paramref name="triggerName"/> can be run.
        /// </summary>
        /// <param name="triggerName">The name of the trigger that relates to a workflow.</param>
        public async Task<LogicAppTriggerUrl> GetTriggerUrlByNameAsync(string triggerName)
        {
            Guard.NotNullOrEmpty(triggerName, nameof(triggerName));
            
            _logger.LogTrace("Request the workflow trigger URL of logic app '{LogicAppName}' in resource group '{ResourceGroup}'", _logicAppName, _resourceGroup);
            WorkflowTriggerCallbackUrl callbackUrl = 
                await _logicManagementClient.WorkflowTriggers.ListCallbackUrlAsync(_resourceGroup, _logicAppName, triggerName);

            return new LogicAppTriggerUrl(callbackUrl.Value, callbackUrl.Method);
        }

        private async Task<string> GetTriggerNameAsync()
        {
            IPage<WorkflowTrigger> triggers = await _logicManagementClient.WorkflowTriggers.ListAsync(_resourceGroup, _logicAppName);
            
            if (triggers.Any())
            {
                return triggers.First().Name;
            }

            throw new LogicAppTriggerNotFoundException(_logicManagementClient.SubscriptionId, _resourceGroup, _logicAppName, $"Cannot find any trigger for logic app '{_logicAppName}' in resource group '{_resourceGroup}'");
        }

        /// <summary>
        /// Temporary enables a static result for an action with the given <paramref name="actionName"/> on the logic app,
        /// and disables the static result when the returned instance gets disposed.
        /// </summary>
        /// <param name="actionName">The name of the action to enable the static result.</param>
        /// <returns>
        ///     An instance to control when the static result for the action on the logic app should be disabled.
        /// </returns>
        public async Task<IAsyncDisposable> TemporaryEnableSuccessStaticResultAsync(string actionName)
        {
            Guard.NotNullOrEmpty(actionName, nameof(actionName));

            var successfulStaticResult = new StaticResultDefinition
            {
                Outputs = new Outputs { Headers = new Dictionary<string, string>(), StatusCode = "OK" },
                Status = "Succeeded"
            };

            return await TemporaryEnableStaticResultAsync(actionName, successfulStaticResult);
        }

        /// <summary>
        /// Temporary enables a static result for an action with the given <paramref name="actionName"/> on the logic app,
        /// and disables the static result when the returned instance gets disposed.
        /// </summary>
        /// <param name="actionName">The name of the action to enable the static result.</param>
        /// <param name="definition">The definition that describes the static result for the action.</param>
        /// <returns>
        ///     An instance to control when the static result for the action on the logic app should be disabled.
        /// </returns>
        public async Task<IAsyncDisposable> TemporaryEnableStaticResultAsync(string actionName, StaticResultDefinition definition)
        {
            Guard.NotNullOrEmpty(actionName, nameof(actionName));
            Guard.NotNull(definition, nameof(definition));

            return await AsyncDisposable.CreateAsync(
                async () => await EnableStaticResultForActionsAsync(new Dictionary<string, StaticResultDefinition> { [actionName] = definition }),
                async () => await DisableStaticResultForActionAsync(actionName));
        }

        /// <summary>
        /// Enables static results for a given set of actions on the logic app.
        /// </summary>
        /// <param name="actions">The set of action names and the corresponding static result.</param>
        /// <returns>
        ///     An instance to control when the static result for the actions on the logic app should be disabled.
        /// </returns>
        public async Task<IAsyncDisposable> TemporaryEnableStaticResultsAsync(IDictionary<string, StaticResultDefinition> actions)
        {
            Guard.NotNull(actions, nameof(actions));
            Guard.NotAny(actions, nameof(actions));

            return await AsyncDisposable.CreateAsync(
                async () => await EnableStaticResultForActionsAsync(actions),
                async () => await DisableStaticResultsForActionsAsync(actions.Keys));
        }

        private async Task EnableStaticResultForActionsAsync(IDictionary<string, StaticResultDefinition> actions)
        {
            Guard.NotNull(actions, nameof(actions));
            Guard.NotAny(actions, nameof(actions));
            Guard.For<ArgumentException>(
                () => actions.Any(action => action.Key is null || action.Value is null),
                "Cannot enable static result for actions when either the action or result is missing");

            string actionNames = String.Join(", ", actions.Keys);
            _logger.LogTrace("Enables (+) a static result definition for actions {ActionNames} of logic app '{LogicAppName}' in resource group '{ResourceGroup}'", actionNames, _logicAppName, _resourceGroup);
            
            Workflow workflow = await _logicManagementClient.Workflows.GetAsync(_resourceGroup, _logicAppName);
            var logicAppDefinition = JsonConvert.DeserializeObject<LogicAppDefinition>(workflow.Definition.ToString());

            foreach (KeyValuePair<string, StaticResultDefinition> action in actions)
            {
                if (logicAppDefinition.Actions[action.Key] is null)
                {
                    continue;
                }

                logicAppDefinition = UpdateLogicAppDefinitionWithStaticResult(logicAppDefinition, action.Key, action.Value);    
            }
            
            workflow.Definition = JObject.Parse(JsonConvert.SerializeObject(logicAppDefinition));

            Workflow resultWorkflow = await _logicManagementClient.Workflows.CreateOrUpdateAsync(_resourceGroup, _logicAppName, workflow);
            if (resultWorkflow.Name != _logicAppName)
            {
                throw new LogicAppNotUpdatedException(_logicManagementClient.SubscriptionId, _resourceGroup, _logicAppName, "Failed to enable a static result.");
            }
        }

        private static LogicAppDefinition UpdateLogicAppDefinitionWithStaticResult(
            LogicAppDefinition logicAppDefinition, 
            string actionName, 
            StaticResultDefinition staticResultDefinition)
        {
            ActionDefinition actionDefinition = logicAppDefinition.Actions[actionName];
            if (actionDefinition.RuntimeConfiguration != null)
            {
                actionDefinition.RuntimeConfiguration.StaticResult.StaticResultOptions = "Enabled";
            }
            else
            {
                string staticResultName = $"{actionName}0";
                actionDefinition.RuntimeConfiguration = new RuntimeConfiguration
                {
                    StaticResult = new StaticResult { Name = staticResultName, StaticResultOptions = "Enabled" }
                };

                if (logicAppDefinition.StaticResults == null)
                {
                    logicAppDefinition.StaticResults = new Dictionary<string, StaticResultDefinition>();
                }

                logicAppDefinition.StaticResults[staticResultName] = staticResultDefinition;
            }

            return logicAppDefinition;
        }

        private async Task DisableStaticResultForActionAsync(string actionName)
        {
            Guard.NotNullOrEmpty(actionName, nameof(actionName));

            _logger.LogTrace(
                "Disables (-) a static result definition for action {ActionName} of logic app '{LogicAppName}' in resource group '{ResourceGroup}'", 
                actionName, _logicAppName, _resourceGroup);
            
            await DisableStaticResultsForActionAsync(name => name == actionName);
        }

        private async Task DisableStaticResultsForActionsAsync(IEnumerable<string> actionNames)
        {
            Guard.NotNull(actionNames, nameof(actionNames));
            Guard.NotAny(actionNames, nameof(actionNames));
            Guard.For<ArgumentException>(
                () => actionNames.Any(action => action is null), 
                "Cannot disable static results for actions when one or more action names are missing");
            
            _logger.LogTrace(
                "Disables (-) a static result definition for actions {ActionNames} of logic app '{LogicAppName}' in resource group '{ResourceGroup}'", 
                actionNames, _logicAppName, _resourceGroup);

            await DisableStaticResultsForActionAsync(actionNames.Contains);
        }

        private async Task DisableStaticResultsForActionAsync(Func<string, bool> shouldDisable)
        {
            Workflow workflow = await _logicManagementClient.Workflows.GetAsync(_resourceGroup, _logicAppName);
            var logicAppDefinition = JsonConvert.DeserializeObject<LogicAppDefinition>(workflow.Definition.ToString());

            foreach (KeyValuePair<string, ActionDefinition> item in logicAppDefinition.Actions)
            {
                ActionDefinition actionDefinition = item.Value;
                if (shouldDisable(item.Key) && actionDefinition.RuntimeConfiguration != null)
                {
                    actionDefinition.RuntimeConfiguration.StaticResult.StaticResultOptions = "Disabled";
                }
            }

            workflow.Definition = JObject.Parse(JsonConvert.SerializeObject(logicAppDefinition));

            Workflow resultWorkflow = await _logicManagementClient.Workflows.CreateOrUpdateAsync(_resourceGroup, _logicAppName, workflow);
            if (resultWorkflow.Name != _logicAppName)
            {
                throw new LogicAppNotUpdatedException(_logicManagementClient.SubscriptionId, _resourceGroup, _logicAppName, "Failed to disable the static results.");
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _logicManagementClient?.Dispose();
        }
    }
}
