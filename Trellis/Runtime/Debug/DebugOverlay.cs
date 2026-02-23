using System;
using System.Collections.Generic;

namespace Trellis.Debug
{
    /// <summary>
    /// Core logic for the debug overlay. Manages sections, commands, and overlay state.
    /// The visual rendering (UI Toolkit) is handled by a separate MonoBehaviour wrapper.
    /// This class provides the data and command infrastructure.
    /// </summary>
    public class DebugOverlay
    {
        private readonly List<IDebugSection> sections = new();
        private readonly Dictionary<string, DebugCommand> commands = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> commandLog = new();
        private bool isVisible;

        private const int MAX_LOG_ENTRIES = 100;

        /// <summary>
        /// True if the overlay is currently visible.
        /// </summary>
        public bool IsVisible => isVisible;

        /// <summary>
        /// Number of registered sections.
        /// </summary>
        public int SectionCount => sections.Count;

        /// <summary>
        /// Number of registered commands.
        /// </summary>
        public int CommandCount => commands.Count;

        /// <summary>
        /// Number of entries in the command log.
        /// </summary>
        public int LogEntryCount => commandLog.Count;

        /// <summary>
        /// Callback invoked when visibility changes. Consuming code wires this to UI.
        /// </summary>
        public Action<bool> OnVisibilityChanged;

        /// <summary>
        /// Toggles overlay visibility.
        /// </summary>
        public void Toggle()
        {
            isVisible = !isVisible;
            OnVisibilityChanged?.Invoke(isVisible);
        }

        /// <summary>
        /// Sets overlay visibility explicitly.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (isVisible != visible)
            {
                isVisible = visible;
                OnVisibilityChanged?.Invoke(isVisible);
            }
        }

        /// <summary>
        /// Registers a debug section.
        /// </summary>
        public void AddSection(IDebugSection section)
        {
            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }

            sections.Add(section);
        }

        /// <summary>
        /// Removes a debug section.
        /// </summary>
        public void RemoveSection(IDebugSection section)
        {
            sections.Remove(section);
        }

        /// <summary>
        /// Returns the section at the given index.
        /// </summary>
        public IDebugSection GetSection(int index)
        {
            if (index < 0 || index >= sections.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return sections[index];
        }

        /// <summary>
        /// Copies all active sections into the provided list.
        /// </summary>
        public void CopyActiveSectionsTo(List<IDebugSection> target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            for (int i = 0; i < sections.Count; i++)
            {
                if (sections[i].IsActive)
                {
                    target.Add(sections[i]);
                }
            }
        }

        /// <summary>
        /// Registers a debug command.
        /// </summary>
        public void RegisterCommand(DebugCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            commands[command.Name] = command;
        }

        /// <summary>
        /// Registers a debug command with name, description, and handler.
        /// </summary>
        public void RegisterCommand(string name, string description, Func<string[], string> handler)
        {
            RegisterCommand(new DebugCommand(name, description, handler));
        }

        /// <summary>
        /// Executes a command string. Format: "commandName arg1 arg2 ..."
        /// Returns the command output, or an error message if the command is not found.
        /// </summary>
        public string ExecuteCommand(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            string trimmed = input.Trim();
            if (trimmed.Length == 0)
            {
                return null;
            }

            // Split into command name and arguments
            string[] tokens = trimmed.Split(' ');
            string commandName = tokens[0];

            string[] args;
            if (tokens.Length > 1)
            {
                args = new string[tokens.Length - 1];
                Array.Copy(tokens, 1, args, 0, args.Length);
            }
            else
            {
                args = Array.Empty<string>();
            }

            if (!commands.TryGetValue(commandName, out DebugCommand command))
            {
                string error = $"Unknown command: {commandName}";
                AddLogEntry($"> {input}");
                AddLogEntry(error);
                return error;
            }

            AddLogEntry($"> {input}");

            string result = command.Handler(args);
            if (result != null)
            {
                AddLogEntry(result);
            }

            return result;
        }

        /// <summary>
        /// Returns a list of all registered command names and descriptions.
        /// Useful for a "help" command.
        /// </summary>
        public void CopyCommandInfoTo(List<string> target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            foreach (var kvp in commands)
            {
                target.Add($"{kvp.Key} - {kvp.Value.Description}");
            }
        }

        /// <summary>
        /// Gets a log entry by index (0 = oldest).
        /// </summary>
        public string GetLogEntry(int index)
        {
            if (index < 0 || index >= commandLog.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return commandLog[index];
        }

        /// <summary>
        /// Clears the command log.
        /// </summary>
        public void ClearLog()
        {
            commandLog.Clear();
        }

        private void AddLogEntry(string entry)
        {
            if (commandLog.Count >= MAX_LOG_ENTRIES)
            {
                commandLog.RemoveAt(0);
            }

            commandLog.Add(entry);
        }
    }
}
