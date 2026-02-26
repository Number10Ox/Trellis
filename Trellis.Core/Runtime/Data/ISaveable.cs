namespace Trellis.Data
{
    /// <summary>
    /// Interface for stores that participate in the save system.
    /// Stores implementing this can be snapshotted and restored by <see cref="SaveManager"/>.
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        /// Unique key identifying this saveable's data in a save slot.
        /// </summary>
        string SaveKey { get; }

        /// <summary>
        /// Captures the current state as a serializable object.
        /// </summary>
        object CaptureState();

        /// <summary>
        /// Restores state from a previously captured object.
        /// </summary>
        void RestoreState(object state);
    }
}
