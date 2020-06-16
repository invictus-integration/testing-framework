using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using GuardNet;

namespace Invictus.Testing.Model
{
    /// <summary>
    /// Represents an action snapshot of the Logic App running on Azure.
    /// </summary>
    public class LogicAppAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppAction"/> class.
        /// </summary>
        public LogicAppAction(
            string name,
            LogicAppActionStatus status,
            object error,
            dynamic inputs,
            dynamic outputs,
            IDictionary<string, string> trackedProperties,
            DateTimeOffset? startTime = null,
            DateTimeOffset? endTime = null)
        {
            Guard.NotNull(name, nameof(name));

            Name = name;
            Status = status;
            Error = error;
            Inputs = inputs;
            Outputs = outputs;

            TrackedProperties = new ReadOnlyDictionary<string, string>(trackedProperties ?? new Dictionary<string, string>());
            StartTime = startTime;
            EndTime = endTime;
        }

        /// <summary>
        /// Gets the name of the action run.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the status of the action run.
        /// Possible values include: 'NotSpecified', 'Paused', 'Running', 'Waiting', 'Succeeded', 'Skipped', 'Suspended', 'Cancelled', 'Failed', 'Faulted', 'TimedOut', 'Aborted', 'Ignored'
        /// </summary>
        public LogicAppActionStatus Status { get; }
        
        /// <summary>
        /// Gets optional the error occurred during the action run.
        /// </summary>
        public object Error { get; }
        
        /// <summary>
        /// Gets the inputs given to the Logic App action run.
        /// </summary>
        public dynamic Inputs { get; }
        
        /// <summary>
        /// Gets the outputs provided by the Logic App action run.
        /// </summary>
        public dynamic Outputs { get; }
        
        /// <summary>
        /// Gets the series of tracked properties during the Logic App action run.
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
