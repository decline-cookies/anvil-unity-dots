using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// Helper class for managing access control to collections across multiple jobs.
    /// </summary>
    /// <remarks>
    /// Unity will handle this for you if you're dealing with <see cref="EntityQuery"/> or <see cref="IComponentData"/>
    /// but if it's just a native collection, you will have to manage your read/write access.
    ///
    /// The <see cref="IJob"/> safety system will throw errors if you handle it incorrectly but that's not a very
    /// efficient way to develop and correct.
    /// </remarks>
    /// <typeparam name="TContext">A type to give context to allow for multiple keys of the same time (int, string etc)
    /// to be used in a project.</typeparam>
    /// <typeparam name="TKey">The type of key to use to get an instance of an <see cref="CollectionAccessController{TContext, TKey}"/></typeparam>
    public static class CollectionAccessControl<TContext, TKey>
    {
        private static readonly Dictionary<TKey, CollectionAccessController<TContext, TKey>> s_Controllers = new Dictionary<TKey, CollectionAccessController<TContext, TKey>>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Dispose();
        }
        
        /// <summary>
        /// Returns an instance of an <see cref="CollectionAccessController{TContext, TKey}"/> for a given key.
        /// Will create a new one if it doesn't already exist.
        /// </summary>
        /// <param name="key">The key to lookup or create the instance of the <see cref="CollectionAccessController{TContext, TKey}"/>.</param>
        /// <returns>The <see cref="CollectionAccessController{TContext,TKey}"/> instance.</returns>
        public static CollectionAccessController<TContext, TKey> GetOrCreate(TKey key = default)
        {
            if (!s_Controllers.TryGetValue(key, out CollectionAccessController<TContext, TKey> accessController))
            {
                accessController = new CollectionAccessController<TContext, TKey>(key);
                s_Controllers.Add(key, accessController);
            }

            return accessController;
        }

        /// <summary>
        /// Removes an instance of <see cref="CollectionAccessController{TContext, TKey}"/> for a given key from the
        /// lookup. If successful, the instance will also be disposed.
        /// </summary>
        /// <param name="key">The key to lookup the instance of the <see cref="CollectionAccessController{TContext, TKey}"/></param>
        /// <returns>True if successfully removed and disposed. False if the key was not found in the lookup.</returns>
        public static bool RemoveAndDispose(TKey key = default)
        {
            if (!s_Controllers.TryGetValue(key, out CollectionAccessController<TContext, TKey> accessController))
            {
                return false;
            }

            s_Controllers.Remove(key);

            if (accessController.IsDisposing || accessController.IsDisposed)
            {
                return true;
            }
            
            accessController.Dispose();

            return true;
        }
        
        /// <summary>
        /// High level dispose to be used when exiting from an ECS/DOTS mode into another part of the application
        /// that doesn't used ECS/DOTS. All controllers will be removed from the lookup and disposed.
        /// </summary>
        public static void Dispose()
        {
            foreach (CollectionAccessController<TContext, TKey> accessController in s_Controllers.Values)
            {
                accessController.Dispose();
            }
            s_Controllers.Clear();
        }
    }
}
