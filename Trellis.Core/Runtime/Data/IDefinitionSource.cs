using System.Collections.Generic;

namespace Trellis.Data
{
    /// <summary>
    /// Interface for loading definitions from a data source (ScriptableObjects, Addressables, JSON, etc.).
    /// Implementations populate a list with definitions. Called once during registry construction.
    /// </summary>
    public interface IDefinitionSource<TDef>
    {
        /// <summary>
        /// Loads all definitions from this source into the provided list.
        /// </summary>
        void LoadDefinitions(List<TDef> results);
    }
}
