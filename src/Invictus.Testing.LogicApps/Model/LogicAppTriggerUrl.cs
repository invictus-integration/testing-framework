using GuardNet;

namespace Invictus.Testing.LogicApps.Model
{
    /// <summary>
    /// Represents the URL of the <see cref="LogicAppTrigger"/>.
    /// </summary>
    public class LogicAppTriggerUrl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppTriggerUrl"/> class.
        /// </summary>
        public LogicAppTriggerUrl(string url, string method)
        {
            Guard.NotNull(url, nameof(url));
            Guard.NotNull(method, nameof(method));

            Url = url;
            Method = method;
        }

        /// <summary>
        /// Gets the URL of the trigger.
        /// </summary>
        public string Url { get; }
        
        /// <summary>
        /// Gets the HTTP method of the trigger.
        /// </summary>
        public string Method { get; }
    }
}
