namespace Trellis.Data
{
    /// <summary>
    /// Pluggable serialization interface for the save system.
    /// Default implementation uses JSON. Consumers can provide MessagePack, binary, etc.
    /// </summary>
    public interface ISaveSerializer
    {
        /// <summary>
        /// Serializes an object to a byte array.
        /// </summary>
        byte[] Serialize<T>(T value);

        /// <summary>
        /// Deserializes a byte array back to an object.
        /// </summary>
        T Deserialize<T>(byte[] data);
    }
}
