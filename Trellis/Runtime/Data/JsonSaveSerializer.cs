using UnityEngine;

namespace Trellis.Data
{
    /// <summary>
    /// Default save serializer using Unity's JsonUtility.
    /// Handles serialization/deserialization of save data to/from JSON bytes.
    /// </summary>
    public class JsonSaveSerializer : ISaveSerializer
    {
        public byte[] Serialize<T>(T value)
        {
            string json = JsonUtility.ToJson(value);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        public T Deserialize<T>(byte[] data)
        {
            string json = System.Text.Encoding.UTF8.GetString(data);
            return JsonUtility.FromJson<T>(json);
        }
    }
}
