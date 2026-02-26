using System;

namespace Trellis.Reactive
{
    /// <summary>
    /// Read-only wrapper around an Observable. Exposes value access and subscription only.
    /// Used by stores to expose state that consumers can read but not write.
    /// </summary>
    public class ReadOnlyObservable<T> : IReadOnlyObservable<T>
    {
        private readonly Observable<T> source;

        public ReadOnlyObservable(Observable<T> source)
        {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>
        /// Current value of the observable.
        /// </summary>
        public T Value => source.Value;

        /// <summary>
        /// Subscribe to value changes. Returns a disposable subscription handle.
        /// The handler is NOT called with the current value on subscribe.
        /// </summary>
        public IDisposable Subscribe(Action<T> onChanged)
        {
            return source.Subscribe(onChanged);
        }
    }
}
