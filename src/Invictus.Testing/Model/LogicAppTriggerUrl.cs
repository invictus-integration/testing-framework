using GuardNet;

namespace Invictus.Testing.Model
{
    /// <summary>
    /// Represents the URL of the <see cref="LogicAppTrigger"/>.
    /// </summary>
    public class LogicAppTriggerUrl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppTriggerUrl"/> class.
        /// </summary>
        public LogicAppTriggerUrl(string value, string method)
        {
            Guard.NotNull(value, nameof(value));
            Guard.NotNull(method, nameof(method));

            Value = value;
            Method = method;
        }

        /// <summary>
        /// Gets the URL of the trigger.
        /// </summary>
        public string Value { get; }
        
        /// <summary>
        /// Gets the HTTP method of the trigger.
        /// </summary>
        public string Method { get; }
    }
}
