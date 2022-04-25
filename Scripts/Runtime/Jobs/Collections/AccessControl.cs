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
    /// <typeparam name="TKey">The type of key to use to get an instance of an <see cref="AccessController"/></typeparam>
    public static class AccessControl<TContext, TKey>
    {
        private static readonly Dictionary<TKey, AccessController> s_Controller = new Dictionary<TKey, AccessController>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            s_Controller.Clear();
        }
        
        /// <summary>
        /// Returns an instance of an <see cref="AccessController"/> for a given key.
        /// </summary>
        /// <param name="key">The key to lookup the instance of <see cref="AccessController"/>. Will create
        /// one if it is not already created.</param>
        /// <returns></returns>
        public static AccessController GetOrCreate(TKey key = default)
        {
            if (!s_Controller.TryGetValue(key, out AccessController accessController))
            {
                accessController = new AccessController();
                s_Controller.Add(key, accessController);
            }

            return accessController;
        }
    }
}
