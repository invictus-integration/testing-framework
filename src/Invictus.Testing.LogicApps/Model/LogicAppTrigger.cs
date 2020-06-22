using System;
using GuardNet;

namespace Invictus.Testing.LogicApps.Model
{
    /// <summary>
    /// Represents the trigger on an Logic App running on Azure.
    /// </summary>
    public class LogicAppTrigger
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppTrigger"/> class.
        /// </summary>
        public LogicAppTrigger(
            string name,
            LogicAppActionStatus status,
            object inputs,
            object outputs,
            object error,
            DateTimeOffset? startTime = null,
            DateTimeOffset? endTime = null)
        {
            Guard.NotNull(name, nameof(name));

            Name = name;
            Status = status;
            Inputs = inputs;
            Outputs = outputs;
            Error = error;
            StartTime = startTime;
            EndTime = endTime;
        }

        /// <summary>
        /// Gets the name of the Logic App trigger.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the current status after the Logic App was triggered.
        /// </summary>
        public LogicAppActionStatus Status { get; set; }

        /// <summary>
        /// Gets the inputs given to the Logic App trigger.
        /// </summary>
        public object Inputs { get; set; }

        /// <summary>
        /// Gets the outputs provided by the Logic App trigger.
        /// </summary>
        public object Outputs { get; set; }

        /// <summary>
        /// Gets the optional error result of the Logic App trigger.
        /// </summary>
        public object Error { get; set; }
        
        /// <summary>
        /// Gets the time when the trigger was started.
        /// </summary>
        public DateTimeOffset? StartTime { get; set; }
        
        /// <summary>
        /// Gets the time when the trigger was ended.
        /// </summary>
        public DateTimeOffset? EndTime { get; set; }
    }
}
