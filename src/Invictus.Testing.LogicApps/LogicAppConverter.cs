using System;
using System.Collections.Generic;
using System.Linq;
using GuardNet;
using Invictus.Testing.LogicApps.Model;
using Microsoft.Azure.Management.Logic.Models;
using Newtonsoft.Json;

namespace Invictus.Testing.LogicApps
{
    /// <summary>
    /// Collection of conversion function to create custom models from Azure SDK models.
    /// </summary>
    public static class LogicAppConverter
    {
        /// <summary>
        /// Convert to <see cref="LogicAppRun"/>.
        /// </summary>
        public static LogicAppRun ToLogicAppRun(WorkflowRun workFlowRun, IEnumerable<LogicAppAction> actions)
        {
            Guard.NotNull(workFlowRun, nameof(workFlowRun));
            Guard.NotNull(actions, nameof(actions));
            Enum.TryParse(workFlowRun.Status, out LogicAppActionStatus status);

            LogicAppTrigger trigger = CreateLogicAppTriggerFrom(workFlowRun.Trigger);
            IDictionary<string, string> trackedProperties = GetAllTrackedProperties(actions);

            return new LogicAppRun(
                workFlowRun.Name,
                status,
                workFlowRun.Error,
                workFlowRun.Correlation?.ClientTrackingId,
                trigger,
                actions,
                trackedProperties,
                workFlowRun.StartTime,
                workFlowRun.EndTime);
        }

        private static LogicAppTrigger CreateLogicAppTriggerFrom(WorkflowRunTrigger workflowRunTrigger)
        {
            Enum.TryParse(workflowRunTrigger.Status, out LogicAppActionStatus status);

            return new LogicAppTrigger(
                workflowRunTrigger.Name,
                status,
                workflowRunTrigger.Inputs,
                workflowRunTrigger.Outputs,
                workflowRunTrigger.Error,
                workflowRunTrigger.StartTime,
                workflowRunTrigger.EndTime);
        }

        /// <summary>
        /// Convert to <see cref="LogicAppAction"/>.
        /// </summary>
        public static LogicAppAction ToLogicAppAction(WorkflowRunAction workflowRunAction, dynamic input, dynamic output)
        {
            Guard.NotNull(param: workflowRunAction, paramName: nameof(workflowRunAction));
            Enum.TryParse(value: workflowRunAction.Status, result: out LogicAppActionStatus status);

            IDictionary<string, string> trackedProperties = DeserializeTrackedProperties(workflowRunAction: workflowRunAction);

            var logicAppAction = new LogicAppAction(
                name: workflowRunAction.Name,
                status: status,
                error: workflowRunAction.Error,
                inputs: input,
                outputs: output,
                trackedProperties: trackedProperties,
                startTime: workflowRunAction.StartTime,
                endTime: workflowRunAction.EndTime);

                return logicAppAction;
        }

        private static IDictionary<string, string> DeserializeTrackedProperties(WorkflowRunAction workflowRunAction)
        {
            if (workflowRunAction.TrackedProperties is null)
            {
                return new Dictionary<string, string>();
            }

            var trackedPropertiesJson = workflowRunAction.TrackedProperties.ToString();
            var trackedProperties = JsonConvert.DeserializeObject<Dictionary<string, string>>(trackedPropertiesJson);

            return trackedProperties;

        }

        private static IDictionary<string, string> GetAllTrackedProperties(IEnumerable<LogicAppAction> actions)
        {
            return actions
                .Where(x => x.TrackedProperties != null)
                .OrderByDescending(x => x.StartTime)
                .SelectMany(a => a.TrackedProperties)
                .GroupBy(x => x.Key)
                .Select(g => g.First())
                .ToDictionary(x => x.Key, x => x.Value);
        }

        /// <summary>
        /// Convert to <see cref="LogicAppMetadata"/>.
        /// </summary>
        public static LogicAppMetadata ToLogicApp(Workflow workflow)
        {
            Guard.NotNull(workflow, nameof(workflow));
            Enum.TryParse(workflow.State, out LogicAppState state);

            return new LogicAppMetadata(
                workflow.Name,
                state,
                workflow.Version,
                workflow.AccessEndpoint,
                workflow.Definition,
                workflow.CreatedTime,
                workflow.ChangedTime);
        }
    }
}
