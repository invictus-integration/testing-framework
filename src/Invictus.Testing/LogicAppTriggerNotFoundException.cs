using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Invictus.Testing
{
    /// <summary>
    /// Exception thrown when no trigger can be found for a given logic app.
    /// </summary>
    [Serializable]
    public class LogicAppTriggerNotFoundException : LogicAppException
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

        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppTriggerNotFoundException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the exception.</param>
        /// <param name="logicAppName">The name of the logic app resource running in Azure.</param>
        /// <param name="resourceGroup">The resource group where the logic app is located.</param>
        /// <param name="subscriptionId">The ID that identifies the subscription on Azure.</param>
        public LogicAppTriggerNotFoundException(
            string message,
            string logicAppName,
            string resourceGroup,
            string subscriptionId) : base(message, logicAppName, resourceGroup, subscriptionId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppTriggerNotFoundException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the exception.</param>
        /// <param name="logicAppName">The name of the logic app resource running in Azure.</param>
        /// <param name="resourceGroup">The resource group where the logic app is located.</param>
        /// <param name="subscriptionId">The ID that identifies the subscription on Azure.</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public LogicAppTriggerNotFoundException(
            string message,
            string logicAppName,
            string resourceGroup,
            string subscriptionId,
            Exception innerException) : base(message, logicAppName, resourceGroup, subscriptionId, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppException"/> class.
        /// </summary>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        protected LogicAppTriggerNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
