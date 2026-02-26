using System;
using System.Collections.Generic;
using UnityEngine;

namespace Trellis.Pooling
{
    /// <summary>
    /// Registry of named object pools. Consumers request pools by key rather than
    /// managing individual <see cref="GameObjectPool"/> instances.
    /// Provides bulk operations (ReturnAll, Clear) for clean teardown on scope disposal.
    /// </summary>
    public class PoolManager : IDisposable
    {
        private readonly Dictionary<string, GameObjectPool> pools = new();
        private readonly Dictionary<string, List<GameObject>> activeObjects = new();
        private bool disposed;

        /// <summary>
        /// Number of registered pools.
        /// </summary>
        public int PoolCount => pools.Count;

        /// <summary>
        /// Registers an existing pool under a string key.
        /// </summary>
        public void RegisterPool(string id, GameObjectPool pool)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Pool ID cannot be null or empty.", nameof(id));
            }

            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            if (pools.ContainsKey(id))
            {
                throw new ArgumentException($"Pool with ID '{id}' is already registered.", nameof(id));
            }

            pools[id] = pool;
            activeObjects[id] = new List<GameObject>();
        }

        /// <summary>
        /// Creates and registers a new pool for the given prefab.
        /// </summary>
        public GameObjectPool CreatePool(string id, GameObject prefab, int initialCapacity, Transform parent = null)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Pool ID cannot be null or empty.", nameof(id));
            }

            if (pools.ContainsKey(id))
            {
                throw new ArgumentException($"Pool with ID '{id}' is already registered.", nameof(id));
            }

            var pool = new GameObjectPool(prefab, initialCapacity, parent);
            pools[id] = pool;
            activeObjects[id] = new List<GameObject>();
            return pool;
        }

        /// <summary>
        /// Retrieves a pool by ID.
        /// </summary>
        public GameObjectPool GetPool(string id)
        {
            ThrowIfDisposed();

            if (pools.TryGetValue(id, out GameObjectPool pool))
            {
                return pool;
            }

            throw new KeyNotFoundException($"No pool registered with ID '{id}'.");
        }

        /// <summary>
        /// Attempts to retrieve a pool by ID.
        /// </summary>
        public bool TryGetPool(string id, out GameObjectPool pool)
        {
            ThrowIfDisposed();
            return pools.TryGetValue(id, out pool);
        }

        /// <summary>
        /// Returns true if a pool is registered with the given ID.
        /// </summary>
        public bool HasPool(string id)
        {
            return pools.ContainsKey(id);
        }

        /// <summary>
        /// Acquires an object from the named pool at the given position.
        /// Tracks the object for bulk ReturnAll operations.
        /// </summary>
        public GameObject Acquire(string poolId, Vector3 position)
        {
            ThrowIfDisposed();

            var pool = GetPool(poolId);
            var instance = pool.Acquire(position);
            activeObjects[poolId].Add(instance);
            return instance;
        }

        /// <summary>
        /// Returns an object to its pool.
        /// </summary>
        public void Return(string poolId, GameObject instance)
        {
            ThrowIfDisposed();

            if (instance == null) return;

            var pool = GetPool(poolId);
            pool.Return(instance);
            activeObjects[poolId].Remove(instance);
        }

        /// <summary>
        /// Returns all active objects across all pools.
        /// </summary>
        public void ReturnAll()
        {
            ThrowIfDisposed();

            foreach (var kvp in activeObjects)
            {
                if (!pools.TryGetValue(kvp.Key, out GameObjectPool pool))
                {
                    continue;
                }

                var active = kvp.Value;
                for (int i = active.Count - 1; i >= 0; i--)
                {
                    if (active[i] != null)
                    {
                        pool.Return(active[i]);
                    }
                }

                active.Clear();
            }
        }

        /// <summary>
        /// Returns all active objects for a specific pool.
        /// </summary>
        public void ReturnAll(string poolId)
        {
            ThrowIfDisposed();

            if (!pools.TryGetValue(poolId, out GameObjectPool pool))
            {
                return;
            }

            if (!activeObjects.TryGetValue(poolId, out List<GameObject> active))
            {
                return;
            }

            for (int i = active.Count - 1; i >= 0; i--)
            {
                if (active[i] != null)
                {
                    pool.Return(active[i]);
                }
            }

            active.Clear();
        }

        /// <summary>
        /// Gets the number of active (acquired) objects for a specific pool.
        /// </summary>
        public int ActiveCount(string poolId)
        {
            if (activeObjects.TryGetValue(poolId, out List<GameObject> active))
            {
                return active.Count;
            }

            return 0;
        }

        /// <summary>
        /// Gets the number of available (pooled) objects for a specific pool.
        /// </summary>
        public int AvailableCount(string poolId)
        {
            if (pools.TryGetValue(poolId, out GameObjectPool pool))
            {
                return pool.AvailableCount;
            }

            return 0;
        }

        /// <summary>
        /// Clears all pools and destroys all pooled objects.
        /// </summary>
        public void Dispose()
        {
            if (disposed) return;

            // Return all active objects before setting disposed flag,
            // since ReturnAll() checks ThrowIfDisposed().
            foreach (var kvp in activeObjects)
            {
                if (!pools.TryGetValue(kvp.Key, out GameObjectPool pool)) continue;

                var active = kvp.Value;
                for (int i = active.Count - 1; i >= 0; i--)
                {
                    if (active[i] != null)
                    {
                        pool.Return(active[i]);
                    }
                }
                active.Clear();
            }

            disposed = true;

            foreach (var pool in pools.Values)
            {
                pool.Clear();
            }

            pools.Clear();
            activeObjects.Clear();
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(PoolManager));
            }
        }
    }
}
