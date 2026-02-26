using System.Collections.Generic;

namespace Trellis.Localization
{
    /// <summary>
    /// ILocalizationService backed by a string dictionary.
    /// The caller provides the data at construction time — Trellis owns
    /// lookup and substitution, the client owns data ingestion.
    /// </summary>
    public sealed class LocalizationService : ILocalizationService
    {
        private readonly Dictionary<string, string> strings;

        /// <summary>
        /// Creates a localization service from pre-loaded key-value pairs.
        /// </summary>
        public LocalizationService(IReadOnlyDictionary<string, string> entries)
        {
            strings = new Dictionary<string, string>();
            if (entries != null)
            {
                foreach (var kvp in entries)
                {
                    strings[kvp.Key] = kvp.Value;
                }
            }
        }

        public string Get(string key)
        {
            return strings.TryGetValue(key, out string value) ? value : key;
        }

        public string Get(string key, string arg0)
        {
            string template = Get(key);
            return template.Replace("{0}", arg0);
        }
    }
}
