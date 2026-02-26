using System;

namespace Trellis.Logging
{
    /// <summary>
    /// Log tags for categorizing log messages. Enables filtering by subsystem.
    /// Framework provides built-in tags; consuming projects can define their own tags
    /// as any int value (cast to LogTag) outside the reserved range (0-99).
    /// </summary>
    public enum LogTag
    {
        /// <summary>
        /// General/uncategorized messages.
        /// </summary>
        General = 0,

        /// <summary>
        /// Core framework infrastructure.
        /// </summary>
        Core = 1,

        /// <summary>
        /// State machine subsystem.
        /// </summary>
        StateMachine = 2,

        /// <summary>
        /// System scheduler subsystem.
        /// </summary>
        Scheduling = 3,

        /// <summary>
        /// Object pooling subsystem.
        /// </summary>
        Pooling = 4,

        /// <summary>
        /// Event bus subsystem.
        /// </summary>
        Events = 5,

        /// <summary>
        /// Reactive properties and observables.
        /// </summary>
        Reactive = 6,

        /// <summary>
        /// State stores.
        /// </summary>
        Stores = 7,

        /// <summary>
        /// UI framework (router, panels, popups).
        /// </summary>
        UI = 8,

        /// <summary>
        /// Scene loading and management.
        /// </summary>
        Scenes = 9,

        /// <summary>
        /// Application lifecycle.
        /// </summary>
        App = 10,

        /// <summary>
        /// Timer subsystem.
        /// </summary>
        Timers = 11,

        /// <summary>
        /// Networking (Netcode integration).
        /// </summary>
        Network = 12,

        /// <summary>
        /// Save/load system.
        /// </summary>
        Save = 13,

        /// <summary>
        /// Definition registry.
        /// </summary>
        Definitions = 14
    }
}
