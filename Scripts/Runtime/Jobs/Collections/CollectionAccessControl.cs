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
    /// <typeparam name="TKey">The type of key to use to get an instance of an <see cref="CollectionAccessController"/></typeparam>
    public static class CollectionAccessControl<TContext, TKey>
    {
        private static readonly Dictionary<TKey, CollectionAccessController> s_Controllers = new Dictionary<TKey, CollectionAccessController>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            s_Controllers.Clear();
        }
        
        /// <summary>
        /// Returns an instance of an <see cref="CollectionAccessController"/> for a given key.
        /// </summary>
        /// <param name="key">The key to lookup the instance of <see cref="CollectionAccessController"/>. Will create
        /// one if it is not already created.</param>
        /// <returns></returns>
        public static CollectionAccessController GetOrCreate(TKey key = default)
        {
            if (!s_Controllers.TryGetValue(key, out CollectionAccessController accessController))
            {
                accessController = new CollectionAccessController();
                s_Controllers.Add(key, accessController);
            }

            return accessController;
        }

        public static bool Remove(TKey key = default, bool shouldDispose = true)
        {
            if (!s_Controllers.TryGetValue(key, out CollectionAccessController accessController))
            {
                return false;
            }

            s_Controllers.Remove(key);

            if (shouldDispose)
            {
                accessController.Dispose();
            }

            return true;
        }
        
        public static void Dispose()
        {
            foreach (CollectionAccessController accessController in s_Controllers.Values)
            {
                accessController.Dispose();
            }
            s_Controllers.Clear();
        }
    }
}
