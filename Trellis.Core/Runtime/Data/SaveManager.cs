using System;
using System.Collections.Generic;

namespace Trellis.Data
{
    /// <summary>
    /// Manages save/load operations across multiple save slots.
    /// Coordinates serialization of registered <see cref="ISaveable"/> instances
    /// and persistence through an <see cref="ISaveStorage"/> backend.
    /// </summary>
    public class SaveManager
    {
        private readonly ISaveSerializer serializer;
        private readonly ISaveStorage storage;
        private readonly List<ISaveable> saveables = new();

        public SaveManager(ISaveSerializer serializer, ISaveStorage storage)
        {
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        /// <summary>
        /// Registers a saveable to participate in save/load operations.
        /// </summary>
        public void Register(ISaveable saveable)
        {
            if (saveable == null)
            {
                throw new ArgumentNullException(nameof(saveable));
            }

            if (string.IsNullOrEmpty(saveable.SaveKey))
            {
                throw new ArgumentException("Saveable must have a non-empty SaveKey.", nameof(saveable));
            }

            saveables.Add(saveable);
        }

        /// <summary>
        /// Unregisters a saveable from save/load operations.
        /// </summary>
        public void Unregister(ISaveable saveable)
        {
            saveables.Remove(saveable);
        }

        /// <summary>
        /// Saves all registered saveables to the specified slot.
        /// </summary>
        public void Save(string slotId)
        {
            if (string.IsNullOrEmpty(slotId))
            {
                throw new ArgumentException("Slot ID cannot be null or empty.", nameof(slotId));
            }

            var slot = new SaveSlot(slotId);

            for (int i = 0; i < saveables.Count; i++)
            {
                var saveable = saveables[i];
                object state = saveable.CaptureState();

                if (state != null)
                {
                    byte[] data = serializer.Serialize(state);
                    slot.SetEntry(saveable.SaveKey, data);
                }
            }

            byte[] slotData = serializer.Serialize(slot);
            storage.Write(slotId, slotData);
        }

        /// <summary>
        /// Loads state from the specified slot and restores all registered saveables.
        /// </summary>
        public bool Load(string slotId)
        {
            if (string.IsNullOrEmpty(slotId))
            {
                throw new ArgumentException("Slot ID cannot be null or empty.", nameof(slotId));
            }

            if (!storage.Exists(slotId))
            {
                return false;
            }

            byte[] slotData = storage.Read(slotId);
            if (slotData == null)
            {
                return false;
            }

            var slot = serializer.Deserialize<SaveSlot>(slotData);

            for (int i = 0; i < saveables.Count; i++)
            {
                var saveable = saveables[i];
                byte[] entryData = slot.GetEntry(saveable.SaveKey);

                if (entryData != null)
                {
                    object state = serializer.Deserialize<object>(entryData);
                    saveable.RestoreState(state);
                }
            }

            return true;
        }

        /// <summary>
        /// Returns true if a save exists in the specified slot.
        /// </summary>
        public bool SlotExists(string slotId)
        {
            if (string.IsNullOrEmpty(slotId))
            {
                return false;
            }

            return storage.Exists(slotId);
        }

        /// <summary>
        /// Deletes the save data in the specified slot.
        /// </summary>
        public void DeleteSlot(string slotId)
        {
            if (string.IsNullOrEmpty(slotId))
            {
                throw new ArgumentException("Slot ID cannot be null or empty.", nameof(slotId));
            }

            storage.Delete(slotId);
        }

        /// <summary>
        /// Number of registered saveables.
        /// </summary>
        public int RegisteredCount => saveables.Count;
    }
}
