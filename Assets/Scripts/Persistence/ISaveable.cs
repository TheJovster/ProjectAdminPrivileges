namespace ProjectAdminPrivileges.Persistence
{
    /// <summary>
    /// Interface for components that need to persist data between sessions.
    /// Implement this to participate in save/load system.
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// Serialize component's state to saveable data object.
        /// Called by SaveWrapper when saving game.
        /// </summary>
        /// <returns>Object containing all data to persist (must be JSON-serializable)</returns>
        object CaptureState();

        /// <summary>
        /// Deserialize and apply saved data to component's state.
        /// Called by SaveWrapper when loading game.
        /// </summary>
        /// <param name="state">Previously saved state object (type must match CaptureState return type)</param>
        void RestoreState(object state);
    }
}