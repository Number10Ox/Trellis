using System;

namespace Trellis.UI
{
    /// <summary>
    /// Describes a popup to be shown by the <see cref="PopupManager"/>.
    /// </summary>
    public class PopupRequest
    {
        /// <summary>
        /// Unique identifier for the popup type.
        /// </summary>
        public string PopupId { get; }

        /// <summary>
        /// If true, blocks input to panels behind the popup and shows a backdrop.
        /// </summary>
        public bool Modal { get; }

        /// <summary>
        /// Optional data to pass to the popup.
        /// </summary>
        public object Data { get; }

        /// <summary>
        /// Callback invoked when the popup is dismissed with a result.
        /// </summary>
        public Action<PopupResult> OnResult { get; }

        public PopupRequest(string popupId, bool modal = true, object data = null, Action<PopupResult> onResult = null)
        {
            PopupId = popupId ?? throw new ArgumentNullException(nameof(popupId));
            Modal = modal;
            Data = data;
            OnResult = onResult;
        }
    }

    /// <summary>
    /// Result returned when a popup is dismissed.
    /// </summary>
    public class PopupResult
    {
        /// <summary>
        /// True if the popup was confirmed (e.g., user clicked OK/Confirm).
        /// </summary>
        public bool Confirmed { get; }

        /// <summary>
        /// Optional result data from the popup.
        /// </summary>
        public object Data { get; }

        public PopupResult(bool confirmed, object data = null)
        {
            Confirmed = confirmed;
            Data = data;
        }

        public static readonly PopupResult Confirm = new PopupResult(true);
        public static readonly PopupResult Cancel = new PopupResult(false);
    }
}
