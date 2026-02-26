namespace Trellis.Localization
{
    /// <summary>
    /// Resolves localization keys to display strings.
    /// Inject into any consumer that needs localized text.
    /// </summary>
    public interface ILocalizationService
    {
        /// <summary>
        /// Returns the localized string for the given key,
        /// or the key itself if not found.
        /// </summary>
        string Get(string key);

        /// <summary>
        /// Returns the localized string with a single substitution ({0}).
        /// </summary>
        string Get(string key, string arg0);
    }
}
