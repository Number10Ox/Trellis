using System;
using System.Collections.Generic;

namespace Trellis.Reactive
{
    /// <summary>
    /// Observable value wrapper that notifies subscribers when the value changes.
    /// Uses queue-based notification to prevent cascading re-entrancy.
    /// </summary>
    public class Observable<T> : IReadOnlyObservable<T>
    {
        private T value;
        private readonly List<Action<T>> subscribers = new();
        private readonly List<Action<T>> pendingRemovals = new();
        private readonly EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        private bool isNotifying;
        private bool hasPendingNotification;
        private T pendingValue;

        public Observable()
        {
            value = default;
        }

        public Observable(T initialValue)
        {
            value = initialValue;
        }

        /// <summary>
        /// Gets or sets the value. Setting triggers notification if the value differs from current.
        /// </summary>
        public T Value
        {
            get => value;
            set
            {
                if (comparer.Equals(this.value, value))
                {
                    return;
                }

                this.value = value;

                if (isNotifying)
                {
                    // Queue the notification for after current notification completes
                    hasPendingNotification = true;
                    pendingValue = value;
                }
                else
                {
                    NotifySubscribers();
                }
            }
        }

        /// <summary>
        /// Sets the value without triggering notifications. Use with caution.
        /// </summary>
        public void SetValueSilent(T newValue)
        {
            value = newValue;
        }

        /// <summary>
        /// Subscribe to value changes. Returns a disposable subscription handle.
        /// The handler is NOT called with the current value on subscribe.
        /// </summary>
        public IDisposable Subscribe(Action<T> onChanged)
        {
            if (onChanged == null)
            {
                throw new ArgumentNullException(nameof(onChanged));
            }

            subscribers.Add(onChanged);
            return new Subscription(this, onChanged);
        }

        /// <summary>
        /// Force a notification to all subscribers with the current value.
        /// </summary>
        public void NotifyAll()
        {
            if (!isNotifying)
            {
                NotifySubscribers();
            }
        }

        private void NotifySubscribers()
        {
            if (subscribers.Count == 0)
            {
                return;
            }

            isNotifying = true;

            T currentValue = value;
            for (int i = 0; i < subscribers.Count; i++)
            {
                subscribers[i].Invoke(currentValue);
            }

            isNotifying = false;
            ProcessPendingRemovals();

            // Process any notification queued during dispatch
            if (hasPendingNotification)
            {
                hasPendingNotification = false;
                T queuedValue = pendingValue;
                pendingValue = default;

                // Only notify if the value at end of notification chain differs
                if (!comparer.Equals(value, queuedValue))
                {
                    // Value changed again during notification, re-notify with current
                    NotifySubscribers();
                }
                else if (!comparer.Equals(currentValue, queuedValue))
                {
                    // The pending value is different from what we just notified
                    value = queuedValue;
                    NotifySubscribers();
                }
            }
        }

        private void Unsubscribe(Action<T> handler)
        {
            if (isNotifying)
            {
                pendingRemovals.Add(handler);
            }
            else
            {
                subscribers.Remove(handler);
            }
        }

        private void ProcessPendingRemovals()
        {
            if (pendingRemovals.Count == 0)
            {
                return;
            }

            for (int i = 0; i < pendingRemovals.Count; i++)
            {
                subscribers.Remove(pendingRemovals[i]);
            }

            pendingRemovals.Clear();
        }

        private sealed class Subscription : IDisposable
        {
            private Observable<T> observable;
            private Action<T> handler;

            public Subscription(Observable<T> observable, Action<T> handler)
            {
                this.observable = observable;
                this.handler = handler;
            }

            public void Dispose()
            {
                if (observable != null)
                {
                    observable.Unsubscribe(handler);
                    observable = null;
                    handler = null;
                }
            }
        }
    }
}
