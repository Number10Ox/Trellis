using System;
using System.Collections.Generic;
using Trellis.Scheduling;

namespace Trellis.UI
{
    /// <summary>
    /// Manages transient toast notifications that auto-dismiss after a configurable duration.
    /// Implements ISystem to be ticked by the SystemScheduler for auto-dismiss timing.
    /// Toasts never block input.
    /// </summary>
    public class ToastManager : ISystem
    {
        private const int DEFAULT_MAX_VISIBLE = 3;

        private readonly List<ActiveToast> activeToasts = new();
        private readonly Queue<ToastRequest> pendingQueue = new();
        private int maxVisible;

        /// <summary>
        /// Callback invoked when a toast should be shown. Consuming code wires this to UI.
        /// Arguments: toast request, toast ID.
        /// </summary>
        public Action<ToastRequest, int> OnShowToast;

        /// <summary>
        /// Callback invoked when a toast should be removed. Arguments: toast ID.
        /// </summary>
        public Action<int> OnHideToast;

        /// <summary>
        /// Maximum number of toasts visible simultaneously.
        /// </summary>
        public int MaxVisible
        {
            get => maxVisible;
            set => maxVisible = value > 0 ? value : 1;
        }

        /// <summary>
        /// Number of toasts currently visible.
        /// </summary>
        public int VisibleCount => activeToasts.Count;

        /// <summary>
        /// Number of toasts waiting in the queue.
        /// </summary>
        public int QueuedCount => pendingQueue.Count;

        private int nextToastId;

        public ToastManager() : this(DEFAULT_MAX_VISIBLE)
        {
        }

        public ToastManager(int maxVisible)
        {
            this.maxVisible = maxVisible > 0 ? maxVisible : DEFAULT_MAX_VISIBLE;
        }

        /// <summary>
        /// Shows a toast notification. If max visible is reached, the toast is queued.
        /// </summary>
        public void Show(ToastRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (activeToasts.Count >= maxVisible)
            {
                pendingQueue.Enqueue(request);
                return;
            }

            ShowToast(request);
        }

        /// <summary>
        /// Advances toast timers and auto-dismisses expired toasts.
        /// Called by SystemScheduler each frame.
        /// </summary>
        public void Tick(float deltaTime)
        {
            // Tick active toasts in reverse so removal doesn't skip elements
            for (int i = activeToasts.Count - 1; i >= 0; i--)
            {
                var toast = activeToasts[i];
                toast.RemainingTime -= deltaTime;

                if (toast.RemainingTime <= 0)
                {
                    OnHideToast?.Invoke(toast.Id);
                    activeToasts.RemoveAt(i);
                }
            }

            // Fill from queue
            while (activeToasts.Count < maxVisible && pendingQueue.Count > 0)
            {
                var next = pendingQueue.Dequeue();
                ShowToast(next);
            }
        }

        /// <summary>
        /// Manually dismisses a toast by ID.
        /// </summary>
        public void Dismiss(int toastId)
        {
            for (int i = 0; i < activeToasts.Count; i++)
            {
                if (activeToasts[i].Id == toastId)
                {
                    OnHideToast?.Invoke(toastId);
                    activeToasts.RemoveAt(i);

                    // Fill from queue
                    if (pendingQueue.Count > 0)
                    {
                        ShowToast(pendingQueue.Dequeue());
                    }

                    return;
                }
            }
        }

        /// <summary>
        /// Dismisses all active toasts and clears the queue.
        /// </summary>
        public void DismissAll()
        {
            for (int i = activeToasts.Count - 1; i >= 0; i--)
            {
                OnHideToast?.Invoke(activeToasts[i].Id);
            }

            activeToasts.Clear();
            pendingQueue.Clear();
        }

        private void ShowToast(ToastRequest request)
        {
            int id = nextToastId++;
            activeToasts.Add(new ActiveToast(id, request.Duration));
            OnShowToast?.Invoke(request, id);
        }

        private class ActiveToast
        {
            public int Id;
            public float RemainingTime;

            public ActiveToast(int id, float duration)
            {
                Id = id;
                RemainingTime = duration;
            }
        }
    }
}
