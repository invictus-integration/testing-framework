namespace Invictus.Testing.Model
{
    /// <summary>
    /// Represents the status in which the <see cref="LogicAppAction"/> is currently at.
    /// </summary>
    public enum LogicAppActionStatus
    {
        /// <summary>
        /// Logic App action run is not specified.
        /// </summary>
        NotSpecified = 0,

        /// <summary>
        /// Logic App action run is paused.
        /// </summary>
        Paused = 1,

        /// <summary>
        /// Logic App action run is running,
        /// </summary>
        Running = 2,

        /// <summary>
        /// Logic App action run is pending.
        /// </summary>
        Waiting = 4,

        /// <summary>
        /// Logic App action run is succeeded.
        /// </summary>
        Succeeded = 8,

        /// <summary>
        /// Logic App action run is skipped.
        /// </summary>
        SKipped = 16,

        /// <summary>
        /// Logic App action run is suspended.
        /// </summary>
        Suspended = 32,

        /// <summary>
        /// Logic App action run is cancelled.
        /// </summary>
        Cancelled = 64,

        /// <summary>
        /// Logic App action run is failed.
        /// </summary>
        Failed = 128,

        /// <summary>
        /// Logic App action run is faulted.
        /// </summary>
        Faulted = 256,

        /// <summary>
        /// Logic App action run is timed out.
        /// </summary>
        TimedOut = 512,

        /// <summary>
        /// Logic App action run is aborted.
        /// </summary>
        Aborted = 1024,

        /// <summary>
        /// Logic App action run is ignored.
        /// </summary>
        Ignored = 2048
    }
}