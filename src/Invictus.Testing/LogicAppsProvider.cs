using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using GuardNet;
using Invictus.Testing.Model;
using Microsoft.Azure.Management.Logic;
using Microsoft.Azure.Management.Logic.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Rest.Azure;
using Microsoft.Rest.Azure.OData;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace Invictus.Testing 
{
    /// <summary>
    /// Component to provide access in a reliable manner on logic app resources running in Azure.
    /// </summary>
    public class LogicAppsProvider
    {
        private readonly string _resourceGroup, _logicAppName;
        private readonly LogicAuthentication _authentication;
        private readonly ILogger _logger;

        private DateTime _startTime = DateTime.UtcNow;
        private TimeSpan _timeout = TimeSpan.FromSeconds(90);
        private string _trackedPropertyName, _trackedPropertyValue, _correlationId;
        private bool _hasTrackedProperty, _hasCorrelationId;

        private static readonly HttpClient HttpClient = new HttpClient();

        private LogicAppsProvider(
            string resourceGroup, 
            string logicAppName,
            LogicAuthentication authentication,
            ILogger logger)
        {
            Guard.NotNullOrWhitespace(resourceGroup, nameof(resourceGroup));
            Guard.NotNullOrWhitespace(logicAppName, nameof(logicAppName));
            Guard.NotNull(authentication, nameof(authentication));
            Guard.NotNull(logger, nameof(logger));

            _resourceGroup = resourceGroup;
            _logicAppName = logicAppName;
            _authentication = authentication;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="LogicAppsProvider"/> class.
        /// </summary>
        /// <param name="resourceGroup">The resource group where the logic apps should be located.</param>
        /// <param name="logicAppName">The name of the logic app resource running in Azure.</param>
        /// <param name="authentication">The authentication mechanism to authenticate with Azure.</param>
        public static LogicAppsProvider LocatedAt(
            string resourceGroup,
            string logicAppName,
            LogicAuthentication authentication)
        {
            Guard.NotNullOrWhitespace(resourceGroup, nameof(resourceGroup));
            Guard.NotNullOrWhitespace(logicAppName, nameof(logicAppName));
            Guard.NotNull(authentication, nameof(authentication));

            return LocatedAt(resourceGroup, logicAppName, authentication, NullLogger.Instance);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="LogicAppsProvider"/> class.
        /// </summary>
        /// <param name="resourceGroup">The resource group where the logic apps should be located.</param>
        /// <param name="logicAppName">The name of the logic app resource running in Azure.</param>
        /// <param name="authentication">The authentication mechanism to authenticate with Azure.</param>
        /// <param name="logger">The instance to write diagnostic trace messages while interacting with the provider.</param>
        public static LogicAppsProvider LocatedAt(
            string resourceGroup,
            string logicAppName,
            LogicAuthentication authentication,
            ILogger logger)
        {
            Guard.NotNullOrWhitespace(resourceGroup, nameof(resourceGroup));
            Guard.NotNullOrWhitespace(logicAppName, nameof(logicAppName));
            Guard.NotNull(authentication, nameof(authentication));
            
            logger = logger ?? NullLogger.Instance;
            return new LogicAppsProvider(resourceGroup, logicAppName, authentication, logger);
        }

        /// <summary>
        /// Sets the start time when the logic app runs were executed.
        /// </summary>
        /// <param name="startTime">The date that the logic app ran.</param>
        public LogicAppsProvider WithStartTime(DateTime startTime)
        {
            // TODO: just migrated from original 'helper' class, should be with offset?
            _startTime = startTime;
            return this;
        }

        /// <summary>
        /// Sets the time period in which the retrieval of the logic app runs should succeed.
        /// </summary>
        /// <param name="timeout">The period to retrieve logic app runs.</param>
        public LogicAppsProvider WithTimeout(TimeSpan timeout)
        {
            _timeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets the correlation ID as client tracking when retrieving logic app runs.
        /// </summary>
        /// <param name="correlationId">The client tracking of the logic app runs.</param>
        public LogicAppsProvider WithCorrelationId(string correlationId)
        {
            Guard.NotNull(correlationId, nameof(correlationId));

            _hasCorrelationId = true;
            _correlationId = correlationId;
            return this;
        }

        /// <summary>
        /// Sets the tracking property as filter when retrieving logic app runs.
        /// </summary>
        /// <param name="trackedPropertyName">The name of the tracked property to filter on.</param>
        /// <param name="trackedPropertyValue">The value of the tracked property to filter on.</param>
        public LogicAppsProvider WithTrackedProperty(string trackedPropertyName, string trackedPropertyValue)
        {
            Guard.NotNull(trackedPropertyName, nameof(trackedPropertyName));
            Guard.NotNull(trackedPropertyValue, nameof(trackedPropertyValue));

            _hasTrackedProperty = true;
            _trackedPropertyName = trackedPropertyName;
            _trackedPropertyValue = trackedPropertyValue;
            return this;
        }

        /// <summary>
        /// Start polling for a single logic app run corresponding to the previously set filtering criteria.
        /// </summary>
        public async Task<LogicAppRun> PollForSingleLogicAppRunAsync()
        {
            IEnumerable<LogicAppRun> logicAppRuns = await PollForLogicAppRunsAsync(numberOfItems: 1);
            return logicAppRuns.FirstOrDefault();
        }

        /// <summary>
        /// Starts polling for a series of logic app runs corresponding to the previously set filtering criteria.
        /// </summary>
        public async Task<IEnumerable<LogicAppRun>> PollForLogicAppRunsAsync()
        {
            return await PollForLogicAppRunsAsync(numberOfItems: -1);
        }

        /// <summary>
        /// Starts polling for a <paramref name="numberOfItems"/> corresponding to the previously set filtering criteria.
        /// </summary>
        /// <param name="numberOfItems">The minimum amount of logic app runs to retrieve.</param>
        public async Task<IEnumerable<LogicAppRun>> PollForLogicAppRunsAsync(int numberOfItems)
        {
            RetryPolicy<IEnumerable<LogicAppRun>> retryPolicy =
                Policy.HandleResult<IEnumerable<LogicAppRun>>(
                          runs => numberOfItems <= 0 ? !runs.Any() : runs.Count() < numberOfItems)
                      .Or<Exception>(ex =>
                      {
                          _logger.LogError(ex, "Polling for logic app runs was faulted: {Message}", ex.Message);
                          return true;
                      })
                      .WaitAndRetryForeverAsync(index =>
                      {
                          _logger.LogTrace("Could not retrieve logic app runs in time, wait 5s and try again...");
                          return TimeSpan.FromSeconds(5);
                      });

            PolicyResult<IEnumerable<LogicAppRun>> result =
                await Policy.TimeoutAsync(_timeout)
                            .WrapAsync(retryPolicy)
                            .ExecuteAndCaptureAsync(GetLogicAppRunsAsync);

            if (result.Outcome == OutcomeType.Failure)
            {
                if (result.FinalException is null
                    || result.FinalException.GetType() == typeof(TimeoutRejectedException))
                {
                    string amount = numberOfItems <= 0 ? "any" : numberOfItems.ToString();
                    _logger.LogError("Polling finished faulted without {Amount} logic app runs", amount);

                    string correlation = _hasCorrelationId
                        ? $"{Environment.NewLine} with correlation property equal '{_correlationId}'"
                        : String.Empty;

                    string trackedProperty = _hasTrackedProperty
                        ? $" {Environment.NewLine} with tracked property [{_trackedPropertyName}] = {_trackedPropertyValue}"
                        : String.Empty;

                    throw new TimeoutException(
                        $"Could not in the given timeout span ({_timeout:g}) retrieve {amount} logic app runs "
                        + $"{Environment.NewLine} with StartTime <= {_startTime:O}"
                        + correlation
                        + trackedProperty);
                }

                throw result.FinalException;
            }

            _logger.LogTrace("Polling finished successful with {LogicAppRunsCount} logic app runs", result.Result.Count());
            return result.Result;
        }

        private async Task<IEnumerable<LogicAppRun>> GetLogicAppRunsAsync()
        {
            using (LogicManagementClient managementClient = await _authentication.AuthenticateAsync())
            {
                var odataQuery = new ODataQuery<WorkflowRunFilter>
                {
                    Filter = $"StartTime ge {_startTime:O} and Status ne 'Running'"
                };

                if (_hasCorrelationId)
                {
                    odataQuery.Filter += $" and ClientTrackingId eq '{_correlationId}'";
                }

                _logger.LogTrace(
                    "Query logic app runs for '{LogicAppName}' in resource group '{ResourceGroup}': {Query}", _logicAppName, _resourceGroup, odataQuery.Filter);
                
                IPage<WorkflowRun> workFlowRuns =
                    await managementClient.WorkflowRuns.ListAsync(_resourceGroup, _logicAppName, odataQuery);

                _logger.LogTrace("Query returned {WorkFlowRunCount} workflow runs", workFlowRuns.Count());

                var logicAppRuns = new Collection<LogicAppRun>();
                foreach (WorkflowRun workFlowRun in workFlowRuns)
                {
                    IEnumerable<LogicAppAction> actions =
                        await FindLogicAppRunActionsAsync(managementClient, workFlowRun.Name);

                    if (_hasTrackedProperty && actions.Any(action => HasTrackedProperty(action.TrackedProperties))
                        || !_hasTrackedProperty)
                    {
                        var logicAppRun = Converter.ToLogicAppRun(workFlowRun, actions);
                        logicAppRuns.Add(logicAppRun);
                    }
                }

                _logger.LogTrace("Query resulted in {LogicAppRunCount} logic app runs", logicAppRuns.Count);
                return logicAppRuns.AsEnumerable(); 
            }
        }

        private async Task<IEnumerable<LogicAppAction>> FindLogicAppRunActionsAsync(ILogicManagementClient managementClient, string runName)
        {
            _logger.LogTrace("Find related logic app run actions...");
            IPage<WorkflowRunAction> workflowRunActions = 
                await managementClient.WorkflowRunActions.ListAsync(_resourceGroup, _logicAppName, runName);

            var actions = new Collection<LogicAppAction>();
            foreach (WorkflowRunAction workflowRunAction in workflowRunActions)
            {
                JToken input = await GetHttpJsonStringAsync(workflowRunAction.InputsLink?.Uri);
                JToken output = await GetHttpJsonStringAsync(workflowRunAction.OutputsLink?.Uri);
                
                var action = Converter.ToLogicAppAction(workflowRunAction, input, output);
                actions.Add(action);
            }

            _logger.LogTrace("Found {LogicAppActionsCount} logic app actions", actions.Count);
            return actions.AsEnumerable();
        }

        private static async Task<JToken> GetHttpJsonStringAsync(string uri)
        {
            if (uri != null)
            {
                string json = await HttpClient.GetStringAsync(uri);
                return JToken.Parse(json);
            }

            return null;
        }

        private bool HasTrackedProperty(IDictionary<string, string> properties)
        {
            if (properties is null || properties.Count <= 0)
            {
                return false;
            }

            return properties.Any(property =>
            {
                if (property.Key is null || property.Value is null)
                {
                    return false;
                }

                return property.Key.Equals(_trackedPropertyName, StringComparison.OrdinalIgnoreCase)
                       && property.Value.Equals(_trackedPropertyValue, StringComparison.OrdinalIgnoreCase);
            });
        }
    }
}