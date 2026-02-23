using System;
using System.Collections.Generic;

namespace Trellis.Data
{
    /// <summary>
    /// Immutable, type-safe registry for game definitions (items, characters, abilities, etc.).
    /// Built once at bootstrap via <see cref="DefinitionRegistryBuilder{TKey,TDef}"/>.
    /// Provides O(1) lookup by key. Not modifiable after construction.
    /// </summary>
    public class DefinitionRegistry<TKey, TDef>
    {
        private readonly Dictionary<TKey, TDef> definitions;

        /// <summary>
        /// Number of definitions in the registry.
        /// </summary>
        public int Count => definitions.Count;

        internal DefinitionRegistry(Dictionary<TKey, TDef> definitions)
        {
            this.definitions = definitions;
        }

        /// <summary>
        /// Attempts to retrieve a definition by key.
        /// </summary>
        public bool TryGet(TKey key, out TDef definition)
        {
            return definitions.TryGetValue(key, out definition);
        }

        /// <summary>
        /// Retrieves a definition by key. Throws if not found.
        /// </summary>
        public TDef Get(TKey key)
        {
            if (definitions.TryGetValue(key, out TDef definition))
            {
                return definition;
            }

            throw new KeyNotFoundException($"Definition not found for key '{key}'.");
        }

        /// <summary>
        /// Returns true if a definition exists for the given key.
        /// </summary>
        public bool Contains(TKey key)
        {
            return definitions.ContainsKey(key);
        }

        /// <summary>
        /// Copies all keys into the provided list. Useful for iteration without exposing internals.
        /// </summary>
        public void CopyKeysTo(List<TKey> target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            foreach (var key in definitions.Keys)
            {
                target.Add(key);
            }
        }

        /// <summary>
        /// Copies all definitions into the provided list. Useful for iteration without exposing internals.
        /// </summary>
        public void CopyValuesTo(List<TDef> target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            foreach (var value in definitions.Values)
            {
                target.Add(value);
            }
        }
    }

    /// <summary>
    /// Builder for constructing an immutable <see cref="DefinitionRegistry{TKey,TDef}"/>.
    /// Validates that all keys are unique and that definitions are not null.
    /// </summary>
    public class DefinitionRegistryBuilder<TKey, TDef>
    {
        private readonly Func<TDef, TKey> keyExtractor;
        private readonly Dictionary<TKey, TDef> definitions;
        private bool built;

        /// <summary>
        /// Creates a builder with a key extraction function.
        /// The extractor pulls the key from each definition (e.g., def => def.Id).
        /// </summary>
        public DefinitionRegistryBuilder(Func<TDef, TKey> keyExtractor)
        {
            this.keyExtractor = keyExtractor ?? throw new ArgumentNullException(nameof(keyExtractor));
            definitions = new Dictionary<TKey, TDef>();
        }

        /// <summary>
        /// Creates a builder with a key extraction function and a custom equality comparer for keys.
        /// </summary>
        public DefinitionRegistryBuilder(Func<TDef, TKey> keyExtractor, IEqualityComparer<TKey> comparer)
        {
            this.keyExtractor = keyExtractor ?? throw new ArgumentNullException(nameof(keyExtractor));
            definitions = new Dictionary<TKey, TDef>(comparer ?? throw new ArgumentNullException(nameof(comparer)));
        }

        /// <summary>
        /// Adds a single definition. The key is extracted automatically.
        /// Throws if the key already exists.
        /// </summary>
        public DefinitionRegistryBuilder<TKey, TDef> Add(TDef definition)
        {
            ThrowIfBuilt();

            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            TKey key = keyExtractor(definition);
            if (definitions.ContainsKey(key))
            {
                throw new ArgumentException($"Duplicate definition key '{key}'.");
            }

            definitions[key] = definition;
            return this;
        }

        /// <summary>
        /// Adds a definition with an explicit key.
        /// Throws if the key already exists.
        /// </summary>
        public DefinitionRegistryBuilder<TKey, TDef> Add(TKey key, TDef definition)
        {
            ThrowIfBuilt();

            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (definitions.ContainsKey(key))
            {
                throw new ArgumentException($"Duplicate definition key '{key}'.");
            }

            definitions[key] = definition;
            return this;
        }

        /// <summary>
        /// Loads definitions from a source. Keys are extracted from each definition.
        /// Throws on duplicate keys.
        /// </summary>
        public DefinitionRegistryBuilder<TKey, TDef> AddSource(IDefinitionSource<TDef> source)
        {
            ThrowIfBuilt();

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var loaded = new List<TDef>();
            source.LoadDefinitions(loaded);

            for (int i = 0; i < loaded.Count; i++)
            {
                TDef def = loaded[i];
                if (def == null)
                {
                    throw new InvalidOperationException($"Definition source produced a null definition at index {i}.");
                }

                TKey key = keyExtractor(def);
                if (definitions.ContainsKey(key))
                {
                    throw new InvalidOperationException($"Duplicate definition key '{key}' from source.");
                }

                definitions[key] = def;
            }

            return this;
        }

        /// <summary>
        /// Builds the immutable registry. The builder cannot be reused after this call.
        /// </summary>
        public DefinitionRegistry<TKey, TDef> Build()
        {
            ThrowIfBuilt();
            built = true;

            // Copy to a new dictionary to ensure immutability
            var copy = new Dictionary<TKey, TDef>(definitions, definitions.Comparer);
            return new DefinitionRegistry<TKey, TDef>(copy);
        }

        private void ThrowIfBuilt()
        {
            if (built)
            {
                throw new InvalidOperationException("Builder has already been used to build a registry.");
            }
        }
    }
}
