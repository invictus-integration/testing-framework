using System;
using GuardNet;

namespace Invictus.Testing.Model
{
    /// <summary>
    /// Represents an Logic App registration running on Azure containing meta-data information.
    /// </summary>
    public class LogicAppMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppMetadata"/> class.
        /// </summary>
        public LogicAppMetadata(
            string name,
            LogicAppState state,
            string version,
            string accessEndpoint,
            dynamic definition,
            DateTimeOffset? createdTime = null,
            DateTimeOffset? changedTime = null)
        {
            Guard.NotNull(name, nameof(name));
            Guard.NotNull(version, nameof(version));

            Name = name;
            State = state;
            Version = version;
            AccessEndpoint = accessEndpoint;
            Definition = definition;
            CreatedTime = createdTime;
            ChangedTime = changedTime;
        }

        /// <summary>
        /// Gets the resource name of the Azure Logic App.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the state of the Azure Logic App.
        /// Possible values include: 'NotSpecified', 'Completed', 'Enabled', 'Disabled', 'Deleted', 'Suspended'.
        /// </summary>
        public LogicAppState State { get; }
        
        /// <summary>
        /// Gets the version of the Azure Logic App.
        /// </summary>
        public string Version { get; }
        
        /// <summary>
        /// Gets the access endpoint of the Azure Logic App.
        /// </summary>
        public string AccessEndpoint { get; }
        
        /// <summary>
        /// Gets the workflow definition of the Azure Logic App.
        /// </summary>
        public dynamic Definition { get; }
        
        /// <summary>
        /// Gets the optional time when the Logic App was created.
        /// </summary>
        public DateTimeOffset? CreatedTime { get; }

        /// <summary>
        /// Gets the optional time when the Logic App was changed.
        /// </summary>
        public DateTimeOffset? ChangedTime { get; }
    }
}
