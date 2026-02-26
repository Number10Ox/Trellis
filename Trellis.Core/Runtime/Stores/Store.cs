using System;
using Trellis.Reactive;

namespace Trellis.Stores
{
    /// <summary>
    /// FLUX-inspired typed data store. Single source of truth for a piece of application state.
    /// Exposes state as ReadOnlyObservable - consumers can read and subscribe, but cannot write directly.
    /// Only StoreActions should modify the store via SetState.
    /// </summary>
    /// <remarks>
    /// DEPRECATED: Prefer mutable state classes with Observable&lt;T&gt; fields instead.
    /// Store + StoreActions creates GC pressure via closures and new state objects on every mutation.
    /// The Observable field pattern achieves the same reactivity with zero allocations per mutation.
    /// See TDD.md Section 2.7 for migration guidance.
    /// </remarks>
    [Obsolete("Use mutable state classes with Observable<T> fields instead. Store/StoreActions creates unnecessary GC allocations per mutation.")]
    public class Store<T>
    {
        private readonly Observable<T> state;
        private readonly T initialValue;

        /// <summary>
        /// Read-only view of the store state. Subscribe to receive change notifications.
        /// </summary>
        public ReadOnlyObservable<T> State { get; }

        /// <summary>
        /// Current state value.
        /// </summary>
        public T CurrentValue => state.Value;

        public Store() : this(default)
        {
        }

        public Store(T initialValue)
        {
            this.initialValue = initialValue;
            state = new Observable<T>(initialValue);
            State = new ReadOnlyObservable<T>(state);
        }

        /// <summary>
        /// Sets the store state. Called by StoreActions, not by consumers directly.
        /// </summary>
        internal void SetState(T newState)
        {
            state.Value = newState;
        }

        /// <summary>
        /// Resets the store to its initial value and notifies subscribers.
        /// </summary>
        public void Reset()
        {
            state.Value = initialValue;
        }
    }
}
