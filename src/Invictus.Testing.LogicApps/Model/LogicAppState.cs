namespace Invictus.Testing.LogicApps.Model
{
    /// <summary>
    /// Represents the state in which the Azure <see cref="LogicApp"/> model can be in.
    /// </summary>
    public enum LogicAppState
    {
        /// <summary>
        /// The state of the Logic App is not specified.
        /// </summary>
        NotSpecified = 0,
        
        /// <summary>
        /// Logic App is completed.
        /// </summary>
        Completed = 1,
        
        /// <summary>
        /// Logic App is enabled.
        /// </summary>
        Enabled = 2,
        
        /// <summary>
        /// Logic App is disabled.
        /// </summary>
        Disabled = 4,
        
        /// <summary>
        /// Logic App is deleted.
        /// </summary>
        Deleted = 8,
        
        /// <summary>
        /// Logic App is suspended.
        /// </summary>
        Suspended = 16
    }
}