using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Invictus.Testing.Model;
using Microsoft.Azure.Management.Logic.Models;
using Newtonsoft.Json;

namespace Invictus.Testing
{
    internal class Converter
    {
        /// <summary>
        /// Convert to <see cref="LogicAppRun"/>.
        /// </summary>
        public static LogicAppRun ToLogicAppRun(WorkflowRun workFlowRun, IEnumerable<LogicAppAction> actions)
        {
            return new LogicAppRun
            {
                Id = workFlowRun.Name,
                StartTime = workFlowRun.StartTime,
                EndTime = workFlowRun.EndTime,
                Status = workFlowRun.Status,
                Error = workFlowRun.Error,
                CorrelationId = workFlowRun.Correlation?.ClientTrackingId,
                Trigger = CreateLogicAppTriggerFrom(workFlowRun.Trigger),
                Actions = actions,
                TrackedProperties = new ReadOnlyDictionary<string, string>(GetAllTrackedProperties(actions))
            };
        }

        private static LogicAppTrigger CreateLogicAppTriggerFrom(WorkflowRunTrigger workflowRunTrigger)
        {
            return new LogicAppTrigger
            {
                Name = workflowRunTrigger.Name,
                Inputs = workflowRunTrigger.Inputs,
                Outputs = workflowRunTrigger.Outputs,
                StartTime = workflowRunTrigger.StartTime,
                EndTime = workflowRunTrigger.EndTime,
                Status = workflowRunTrigger.Status,
                Error = workflowRunTrigger.Error
            };
        }

        /// <summary>
        /// Convert to <see cref="LogicAppAction"/>.
        /// </summary>
        public static LogicAppAction ToLogicAppAction(WorkflowRunAction workflowRunAction, string input, string output)
        {
            var logicAppAction = new LogicAppAction
            {
                Name = workflowRunAction.Name,
                StartTime = workflowRunAction.StartTime,
                EndTime = workflowRunAction.EndTime,
                Status = workflowRunAction.Status,
                Error = workflowRunAction.Error,
                Inputs = input,
                Outputs = output
            };

            if (workflowRunAction.TrackedProperties != null)
            {
                logicAppAction.TrackedProperties =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(
                        workflowRunAction.TrackedProperties.ToString());
            }

            return logicAppAction;
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
        /// Convert to <see cref="LogicApp"/>.
        /// </summary>
        public static LogicApp ToLogicApp(Workflow workflow)
        {
            return new LogicApp
            {
                Name = workflow.Name,
                CreatedTime = workflow.CreatedTime,
                ChangedTime = workflow.ChangedTime,
                State = workflow.State,
                Version = workflow.Version,
                AccessEndpoint = workflow.AccessEndpoint,
                Definition = workflow.Definition
            };
        }
    }
}
