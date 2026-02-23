using System;

namespace Trellis.Debug
{
    /// <summary>
    /// A registered debug command that can be executed from the debug overlay's command input.
    /// </summary>
    public class DebugCommand
    {
        /// <summary>
        /// The command name (e.g., "set", "goto", "spawn"). Case-insensitive matching.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Description of what the command does.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The handler that executes when the command is invoked.
        /// Arguments are the space-separated tokens after the command name.
        /// Returns a result string to display in the overlay (can be null).
        /// </summary>
        public Func<string[], string> Handler { get; }

        public DebugCommand(string name, string description, Func<string[], string> handler)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? string.Empty;
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }
    }
}
