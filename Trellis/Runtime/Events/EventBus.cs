using System;
using System.Collections.Generic;

namespace Trellis.Events
{
    /// <summary>
    /// Typed event bus for decoupled system-to-system communication.
    /// Events are struct values dispatched to all subscribers in FIFO order.
    /// </summary>
    public class EventBus
    {
        private interface ISubscriptionList
        {
            void ProcessPendingRemovals();
        }

        private sealed class SubscriptionList<T> : ISubscriptionList where T : struct
        {
            private readonly List<Action<T>> handlers = new();
            private readonly List<Action<T>> pendingRemovals = new();
            private bool isDispatching;

            public int Count => handlers.Count;

            public IEventSubscription Subscribe(Action<T> handler)
            {
                if (handler == null)
                {
                    throw new ArgumentNullException(nameof(handler));
                }

                handlers.Add(handler);
                return new Subscription(this, handler);
            }

            public void Publish(T evt)
            {
                if (handlers.Count == 0)
                {
                    return;
                }

                isDispatching = true;

                for (int i = 0; i < handlers.Count; i++)
                {
                    handlers[i].Invoke(evt);
                }

                isDispatching = false;
                ProcessPendingRemovals();
            }

            public void Unsubscribe(Action<T> handler)
            {
                if (isDispatching)
                {
                    pendingRemovals.Add(handler);
                }
                else
                {
                    handlers.Remove(handler);
                }
            }

            public void ProcessPendingRemovals()
            {
                if (pendingRemovals.Count == 0)
                {
                    return;
                }

                for (int i = 0; i < pendingRemovals.Count; i++)
                {
                    handlers.Remove(pendingRemovals[i]);
                }

                pendingRemovals.Clear();
            }

            private sealed class Subscription : IEventSubscription
            {
                private SubscriptionList<T> list;
                private Action<T> handler;

                public Subscription(SubscriptionList<T> list, Action<T> handler)
                {
                    this.list = list;
                    this.handler = handler;
                }

                public void Dispose()
                {
                    if (list != null)
                    {
                        list.Unsubscribe(handler);
                        list = null;
                        handler = null;
                    }
                }
            }
        }

        private readonly Dictionary<Type, object> subscriptionLists = new();

        /// <summary>
        /// Subscribe to events of type T. Returns a disposable subscription handle.
        /// </summary>
        public IEventSubscription Subscribe<T>(Action<T> handler) where T : struct
        {
            return GetOrCreateList<T>().Subscribe(handler);
        }

        /// <summary>
        /// Publish an event to all subscribers. Dispatch order matches subscription order (FIFO).
        /// Publishing with no subscribers is a no-op with no allocations.
        /// </summary>
        public void Publish<T>(T evt) where T : struct
        {
            if (subscriptionLists.TryGetValue(typeof(T), out object listObj))
            {
                ((SubscriptionList<T>)listObj).Publish(evt);
            }
        }

        /// <summary>
        /// Returns the number of active subscribers for event type T.
        /// </summary>
        public int SubscriberCount<T>() where T : struct
        {
            if (subscriptionLists.TryGetValue(typeof(T), out object listObj))
            {
                return ((SubscriptionList<T>)listObj).Count;
            }

            return 0;
        }

        private SubscriptionList<T> GetOrCreateList<T>() where T : struct
        {
            var type = typeof(T);
            if (!subscriptionLists.TryGetValue(type, out object listObj))
            {
                listObj = new SubscriptionList<T>();
                subscriptionLists[type] = listObj;
            }

            return (SubscriptionList<T>)listObj;
        }
    }
}
