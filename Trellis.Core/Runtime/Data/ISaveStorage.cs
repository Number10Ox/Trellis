namespace Trellis.Data
{
    /// <summary>
    /// Abstraction over the storage backend for save data.
    /// Default implementation uses the file system. Consumers can provide
    /// platform-specific implementations (cloud saves, etc.).
    /// </summary>
    public interface ISaveStorage
    {
        /// <summary>
        /// Returns true if data exists for the given slot.
        /// </summary>
        bool Exists(string slotId);

        /// <summary>
        /// Writes data to the given slot.
        /// </summary>
        void Write(string slotId, byte[] data);

        /// <summary>
        /// Reads data from the given slot. Returns null if the slot does not exist.
        /// </summary>
        byte[] Read(string slotId);

        /// <summary>
        /// Deletes data for the given slot.
        /// </summary>
        void Delete(string slotId);
    }
}
