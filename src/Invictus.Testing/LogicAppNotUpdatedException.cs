using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace Invictus.Testing
{
    /// <summary>
    /// Thrown when the logic app running in Azure could not be updated.
    /// </summary>
    [Serializable]
    public class LogicAppNotUpdatedException : LogicAppException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppNotUpdatedException"/> class.
        /// </summary>
        public LogicAppNotUpdatedException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppNotUpdatedException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the exception.</param>
        public LogicAppNotUpdatedException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppNotUpdatedException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public LogicAppNotUpdatedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppTriggerNotFoundException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the exception.</param>
        /// <param name="logicAppName">The name of the logic app resource running in Azure.</param>
        /// <param name="resourceGroup">The resource group where the logic app is located.</param>
        /// <param name="subscriptionId">The ID that identifies the subscription on Azure.</param>
        public LogicAppNotUpdatedException(
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
        public LogicAppNotUpdatedException(
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
        protected LogicAppNotUpdatedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
