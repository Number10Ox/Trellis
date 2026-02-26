using System;

namespace Trellis.Stores
{
    /// <summary>
    /// Base class for store action classes. Provides mutation access to a Store.
    /// Each Store should have exactly one StoreActions class that performs all mutations.
    /// </summary>
    /// <remarks>
    /// DEPRECATED: Prefer mutable state classes with Observable&lt;T&gt; fields instead.
    /// UpdateState(Func&lt;T,T&gt;) allocates a closure + delegate + new state object per call.
    /// See TDD.md Section 2.7 for migration guidance.
    /// </remarks>
    [Obsolete("Use mutable state classes with Observable<T> fields instead. StoreActions creates unnecessary GC allocations per mutation.")]
    public abstract class StoreActions<T>
    {
        private readonly Store<T> store;

        protected StoreActions(Store<T> store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>
        /// Updates the store state by applying a transformation function.
        /// </summary>
        protected void UpdateState(Func<T, T> updater)
        {
            if (updater == null)
            {
                throw new ArgumentNullException(nameof(updater));
            }

            T currentState = store.CurrentValue;
            T newState = updater(currentState);
            store.SetState(newState);
        }

        /// <summary>
        /// Sets the store state directly.
        /// </summary>
        protected void SetState(T newState)
        {
            store.SetState(newState);
        }

        /// <summary>
        /// Gets the current store state.
        /// </summary>
        protected T GetState()
        {
            return store.CurrentValue;
        }
    }
}
