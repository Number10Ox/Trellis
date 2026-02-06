namespace Trellis.Stores
{
    /// <summary>
    /// Loading state for async operations.
    /// </summary>
    public enum LoadState
    {
        /// <summary>
        /// No loading operation in progress or completed.
        /// </summary>
        None,

        /// <summary>
        /// Currently reading/loading data.
        /// </summary>
        Reading,

        /// <summary>
        /// Currently writing/saving data.
        /// </summary>
        Writing,

        /// <summary>
        /// An error occurred during the last operation.
        /// </summary>
        Error
    }
}
