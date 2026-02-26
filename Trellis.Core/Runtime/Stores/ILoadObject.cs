namespace Trellis.Stores
{
    /// <summary>
    /// Wraps a value with its loading state. Replaces boolean flag patterns
    /// (isLoading, hasData, hasError) with a single state machine.
    /// </summary>
    public interface ILoadObject<out T>
    {
        /// <summary>
        /// The wrapped value. May be default if State is not None.
        /// </summary>
        T Value { get; }

        /// <summary>
        /// Current loading state.
        /// </summary>
        LoadState State { get; }

        /// <summary>
        /// Error message if State is Error, null otherwise.
        /// </summary>
        string ErrorMessage { get; }

        /// <summary>
        /// True if State is Reading or Writing.
        /// </summary>
        bool IsLoading { get; }

        /// <summary>
        /// True if State is None and Value is available.
        /// </summary>
        bool HasValue { get; }

        /// <summary>
        /// True if State is Error.
        /// </summary>
        bool HasError { get; }
    }
}
