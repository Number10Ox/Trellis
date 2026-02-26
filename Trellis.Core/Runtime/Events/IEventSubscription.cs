using System;

namespace Trellis.Events
{
    /// <summary>
    /// Handle for an event subscription. Dispose to unsubscribe.
    /// </summary>
    public interface IEventSubscription : IDisposable
    {
    }
}
