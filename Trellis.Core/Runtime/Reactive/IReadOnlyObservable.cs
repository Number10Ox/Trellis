using System;

namespace Trellis.Reactive
{
    /// <summary>
    /// Read-only view of an observable value. Provides value access and subscription only.
    /// </summary>
    public interface IReadOnlyObservable<T>
    {
        /// <summary>
        /// Current value of the observable.
        /// </summary>
        T Value { get; }

        /// <summary>
        /// Subscribe to value changes. The handler is NOT called with the current value on subscribe.
        /// Returns a disposable subscription handle.
        /// </summary>
        IDisposable Subscribe(Action<T> onChanged);
    }
}
