using System;

namespace Invictus.Testing
{
    /// <summary>
    /// Exception thrown when no trigger can be found for a given logic app.
    /// </summary>
    [Serializable]
    public class LogicAppTriggerNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppTriggerNotFoundException"/> class.
        /// </summary>
        public LogicAppTriggerNotFoundException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppTriggerNotFoundException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the exception.</param>
        public LogicAppTriggerNotFoundException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppTriggerNotFoundException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public LogicAppTriggerNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
