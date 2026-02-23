using System;
using System.Collections.Generic;

namespace Trellis.Data
{
    /// <summary>
    /// Represents a named save slot containing serialized state from multiple saveables.
    /// Each slot holds a dictionary of save key → serialized bytes.
    /// </summary>
    [Serializable]
    public class SaveSlot
    {
        private readonly Dictionary<string, byte[]> entries = new();

        /// <summary>
        /// The slot identifier (e.g., "slot1", "autosave").
        /// </summary>
        public string SlotId { get; }

        /// <summary>
        /// Number of entries in this slot.
        /// </summary>
        public int EntryCount => entries.Count;

        public SaveSlot(string slotId)
        {
            SlotId = slotId ?? throw new ArgumentNullException(nameof(slotId));
        }

        /// <summary>
        /// Stores serialized data for a save key.
        /// </summary>
        public void SetEntry(string key, byte[] data)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Save key cannot be null or empty.", nameof(key));
            }

            entries[key] = data;
        }

        /// <summary>
        /// Retrieves serialized data for a save key. Returns null if not found.
        /// </summary>
        public byte[] GetEntry(string key)
        {
            entries.TryGetValue(key, out byte[] data);
            return data;
        }

        /// <summary>
        /// Returns true if an entry exists for the given key.
        /// </summary>
        public bool HasEntry(string key)
        {
            return entries.ContainsKey(key);
        }

        /// <summary>
        /// Removes an entry by key.
        /// </summary>
        public void RemoveEntry(string key)
        {
            entries.Remove(key);
        }

        /// <summary>
        /// Clears all entries from this slot.
        /// </summary>
        public void Clear()
        {
            entries.Clear();
        }

        /// <summary>
        /// Copies all keys into the provided list.
        /// </summary>
        public void CopyKeysTo(List<string> target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            foreach (var key in entries.Keys)
            {
                target.Add(key);
            }
        }
    }
}
