using System;
using System.Collections.Generic;

namespace Trellis.UI
{
    /// <summary>
    /// Queue-based popup manager. Only one modal popup displays at a time.
    /// When dismissed, the next queued popup appears.
    /// </summary>
    public class PopupManager
    {
        private readonly Dictionary<string, IPopup> registeredPopups = new();
        private readonly Queue<PopupRequest> queue = new();
        private PopupRequest activeRequest;
        private bool isShowingModal;

        /// <summary>
        /// Callback for showing/hiding the modal backdrop. Consuming code wires this to UI.
        /// </summary>
        public Action<bool> OnBackdropChanged;

        /// <summary>
        /// True if a modal popup is currently showing.
        /// </summary>
        public bool IsModalActive => isShowingModal;

        /// <summary>
        /// Number of popups waiting in the queue (not including the active popup).
        /// </summary>
        public int QueueCount => queue.Count;

        /// <summary>
        /// True if there is an active popup being shown.
        /// </summary>
        public bool HasActivePopup => activeRequest != null;

        /// <summary>
        /// The ID of the currently active popup, or null if none.
        /// </summary>
        public string ActivePopupId => activeRequest?.PopupId;

        /// <summary>
        /// Registers a popup implementation that can be shown by ID.
        /// </summary>
        public void RegisterPopup(string popupId, IPopup popup)
        {
            if (string.IsNullOrEmpty(popupId))
            {
                throw new ArgumentException("Popup ID cannot be null or empty.", nameof(popupId));
            }

            if (popup == null)
            {
                throw new ArgumentNullException(nameof(popup));
            }

            registeredPopups[popupId] = popup;
        }

        /// <summary>
        /// Shows a popup. If a modal popup is already showing, the request is queued.
        /// </summary>
        public void Show(PopupRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!registeredPopups.ContainsKey(request.PopupId))
            {
                throw new InvalidOperationException($"No popup registered with ID '{request.PopupId}'.");
            }

            if (isShowingModal)
            {
                queue.Enqueue(request);
                return;
            }

            ShowPopup(request);
        }

        /// <summary>
        /// Dismisses the active popup with the given result.
        /// If there are queued popups, the next one is shown.
        /// </summary>
        public void Dismiss(PopupResult result)
        {
            if (activeRequest == null)
            {
                return;
            }

            var request = activeRequest;
            HideActivePopup();

            request.OnResult?.Invoke(result);

            // Show next queued popup if any
            if (queue.Count > 0)
            {
                var next = queue.Dequeue();
                ShowPopup(next);
            }
        }

        /// <summary>
        /// Dismisses the active popup as cancelled and clears the queue.
        /// </summary>
        public void DismissAll()
        {
            if (activeRequest != null)
            {
                var request = activeRequest;
                HideActivePopup();
                request.OnResult?.Invoke(PopupResult.Cancel);
            }

            while (queue.Count > 0)
            {
                var request = queue.Dequeue();
                request.OnResult?.Invoke(PopupResult.Cancel);
            }
        }

        private void ShowPopup(PopupRequest request)
        {
            activeRequest = request;

            if (request.Modal)
            {
                isShowingModal = true;
                OnBackdropChanged?.Invoke(true);
            }

            if (registeredPopups.TryGetValue(request.PopupId, out IPopup popup))
            {
                popup.Show();
            }
        }

        private void HideActivePopup()
        {
            if (activeRequest == null) return;

            if (registeredPopups.TryGetValue(activeRequest.PopupId, out IPopup popup))
            {
                popup.Hide();
            }

            if (isShowingModal)
            {
                isShowingModal = false;
                OnBackdropChanged?.Invoke(false);
            }

            activeRequest = null;
        }
    }
}
