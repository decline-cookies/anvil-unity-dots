using System;
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
    public static class CollectionAccessControl
    {
        //*************************************************************************************************************
        // INTERNAL INTERFACES
        //*************************************************************************************************************

        internal interface ICollectionAccessControl<TKey> : ICollectionAccessControl
        {
            CollectionAccessController<TKey> GetOrCreate(TKey key = default);
            void Remove(CollectionAccessController<TKey> collectionAccessController);
        }

        internal interface ICollectionAccessControl
        {
            void RemoveAndDisposeAll();
        }

        //*************************************************************************************************************
        // INTERNAL HELPER
        //*************************************************************************************************************

        private class CollectionAccessControlImpl<TContext, TKey> : ICollectionAccessControl<TKey>
        {
            private readonly Dictionary<TKey, CollectionAccessController<TKey>> m_Controllers = new Dictionary<TKey, CollectionAccessController<TKey>>();

            private bool m_IsRemovingAndDisposingAll;

            internal CollectionAccessControlImpl()
            {
            }

            public CollectionAccessController<TKey> GetOrCreate(TKey key = default)
            {
                if (!m_Controllers.TryGetValue(key, out CollectionAccessController<TKey> collectionAccessController))
                {
                    collectionAccessController = new CollectionAccessController<TKey>(this, key);
                    m_Controllers.Add(key, collectionAccessController);
                }

                return collectionAccessController;
            }

            public void Remove(CollectionAccessController<TKey> collectionAccessController)
            {
                if (m_IsRemovingAndDisposingAll)
                {
                    return;
                }

                m_Controllers.Remove(collectionAccessController.Key);
            }

            void ICollectionAccessControl.RemoveAndDisposeAll()
            {
                m_IsRemovingAndDisposingAll = true;
                foreach (IDisposable collectionAccessController in m_Controllers.Values)
                {
                    collectionAccessController.Dispose();
                }

                m_Controllers.Clear();
                m_IsRemovingAndDisposingAll = false;
            }
        }

        //*************************************************************************************************************
        // PUBLIC STATIC API
        //*************************************************************************************************************

        private static readonly Dictionary<Type, ICollectionAccessControl> s_AccessControls = new Dictionary<Type, ICollectionAccessControl>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            RemoveAndDisposeAll();
        }

        /// <summary>
        /// Returns an instance of an <see cref="CollectionAccessController{TKey}"/> for a given key.
        /// Will create a new one if it doesn't already exist.
        /// </summary>
        /// <typeparam name="TContext">A type to give context to allow for multiple keys of the same time (int, string etc)
        /// to be used in a project.</typeparam>
        /// <typeparam name="TKey">The type of key to use to get an instance of an <see cref="CollectionAccessController{TKey}"/></typeparam>
        /// <param name="key">The actual key to use</param>
        /// <returns>The <see cref="CollectionAccessController{TKey}"/> instance.</returns>
        public static CollectionAccessController<TKey> GetOrCreate<TContext, TKey>(TKey key = default)
        {
            Type contextType = typeof(TContext);

            if (!s_AccessControls.TryGetValue(contextType, out ICollectionAccessControl collectionAccessControl))
            {
                collectionAccessControl = new CollectionAccessControlImpl<TContext, TKey>();
                s_AccessControls.Add(contextType, collectionAccessControl);
            }

            return ((ICollectionAccessControl<TKey>)collectionAccessControl).GetOrCreate(key);
        }

        /// <summary>
        /// High level dispose to be used when exiting from an ECS/DOTS mode into another part of the application
        /// that doesn't used ECS/DOTS. All controllers will be removed from the lookup and disposed.
        /// </summary>
        public static void RemoveAndDisposeAll()
        {
            foreach (ICollectionAccessControl collectionAccessControl in s_AccessControls.Values)
            {
                collectionAccessControl.RemoveAndDisposeAll();
            }

            s_AccessControls.Clear();
        }
    }
}
