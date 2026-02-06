namespace Trellis.Stores
{
    /// <summary>
    /// Immutable implementation of ILoadObject. Create instances via static factory methods.
    /// </summary>
    public readonly struct LoadObject<T> : ILoadObject<T>
    {
        private readonly T value;
        private readonly LoadState state;
        private readonly string errorMessage;

        private LoadObject(T value, LoadState state, string errorMessage)
        {
            this.value = value;
            this.state = state;
            this.errorMessage = errorMessage;
        }

        public T Value => value;
        public LoadState State => state;
        public string ErrorMessage => errorMessage;
        public bool IsLoading => state == LoadState.Reading || state == LoadState.Writing;
        public bool HasValue => state == LoadState.None;
        public bool HasError => state == LoadState.Error;

        /// <summary>
        /// Creates a LoadObject with the given value and None state.
        /// </summary>
        public static LoadObject<T> WithValue(T value)
        {
            return new LoadObject<T>(value, LoadState.None, null);
        }

        /// <summary>
        /// Creates a LoadObject in Reading state.
        /// </summary>
        public static LoadObject<T> Reading()
        {
            return new LoadObject<T>(default, LoadState.Reading, null);
        }

        /// <summary>
        /// Creates a LoadObject in Writing state.
        /// </summary>
        public static LoadObject<T> Writing()
        {
            return new LoadObject<T>(default, LoadState.Writing, null);
        }

        /// <summary>
        /// Creates a LoadObject in Error state with the given message.
        /// </summary>
        public static LoadObject<T> WithError(string errorMessage)
        {
            return new LoadObject<T>(default, LoadState.Error, errorMessage ?? string.Empty);
        }

        /// <summary>
        /// Creates an empty LoadObject in None state with default value.
        /// </summary>
        public static LoadObject<T> Empty()
        {
            return new LoadObject<T>(default, LoadState.None, null);
        }
    }
}
