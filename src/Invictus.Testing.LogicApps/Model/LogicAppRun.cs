using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardNet;

namespace Invictus.Testing.LogicApps.Model
{
    /// <summary>
    /// Represents a snapshot of a Logic App run located on Azure.
    /// </summary>
    public class LogicAppRun
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppRun"/> class.
        /// </summary>
        public LogicAppRun(
            string id,
            LogicAppActionStatus status,
            object error,
            string correlationId,
            LogicAppTrigger trigger,
            IEnumerable<LogicAppAction> actions,
            IDictionary<string, string> trackedProperties,
            DateTimeOffset? startTime = null,
            DateTimeOffset? endTime = null)
        {
            Guard.NotNull(id, nameof(id));
            actions = actions ?? Enumerable.Empty<LogicAppAction>();
            Guard.For<ArgumentException>(() => actions.Any(action => action is null), "One or more actions is 'null'");
            trackedProperties = trackedProperties ?? new Dictionary<string, string>();
            Guard.For<ArgumentException>(() => trackedProperties.Any(prop => prop.Key is null), "One or more tracked properties has 'null' as 'Key'");

            Id = id;
            Status = status;
            Error = error;
            CorrelationId = correlationId;
            Trigger = trigger;
            Actions = actions;
            TrackedProperties = new ReadOnlyDictionary<string, string>(trackedProperties);
            StartTime = startTime;
            EndTime = endTime;
        }

        /// <summary>
        /// Gets the workflow name.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the status of the Logic App run.
        /// </summary>
        public LogicAppActionStatus Status { get; }
        
        /// <summary>
        /// Gets the optional error occured during the Logic App run.
        /// </summary>
        public object Error { get; }
        
        /// <summary>
        /// Gets the client tracking ID for the Logic App run.
        /// </summary>
        public string CorrelationId { get; }
        
        /// <summary>
        /// Gets the workflow run trigger.
        /// </summary>
        public LogicAppTrigger Trigger { get; }
        
        /// <summary>
        /// Gets the actions executed during this Logic App run.
        /// </summary>
        public IEnumerable<LogicAppAction> Actions { get; }
        
        /// <summary>
        /// Gets the tracked properties for this Logic App run.
        /// </summary>
        public IReadOnlyDictionary<string, string> TrackedProperties { get; }
        
        /// <summary>
        /// Gets the optional time when the Logic App was started.
        /// </summary>
        public DateTimeOffset? StartTime { get; }
        
        /// <summary>
        /// Gets the optional time when the Logic App was ended.
        /// </summary>
        public DateTimeOffset? EndTime { get; }
    }
}
