using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Represents a cached state of a <see cref="ComponentSystemBase"/>
    /// </summary>
    public class SystemCache
    {
        /// <summary>
        /// The <see cref="ComponentSystemBase"/> this cache represents
        /// </summary>
        public ComponentSystemBase System { get; }

        /// <summary>
        /// The <see cref="ComponentType"/>s used in any <see cref="EntityQuery"/>s
        /// on the <see cref="System"/>
        /// </summary>
        public HashSet<ComponentType> QueryComponentTypes { get; } = new HashSet<ComponentType>();

        /// <summary>
        /// The number of <see cref="EntityQuery"/>s this <see cref="System"/> has.
        /// </summary>
        public int QueryCount { get; private set; }

        private readonly HashSet<SystemGroupCache> m_GroupCaches = new HashSet<SystemGroupCache>();

        internal SystemCache(SystemGroupCache groupCache, ComponentSystemBase system)
        {
            m_GroupCaches.Add(groupCache);
            System = system;
        }

        internal void ClearGroups()
        {
            m_GroupCaches.Clear();
        }

        internal virtual void RebuildIfNeeded(SystemGroupCache parentGroupCache)
        {
            //Will ignore if already added
            m_GroupCaches.Add(parentGroupCache);
            int currentQueryCount = System.EntityQueries.Length;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (currentQueryCount < QueryCount)
            {
                throw new InvalidOperationException($"System queries decreased!");
            }
#endif

            if (currentQueryCount > QueryCount)
            {
                RebuildQueries();
            }
        }

        private void RebuildQueries()
        {
            QueryComponentTypes.Clear();
            EntityQuery[] queries = System.EntityQueries;
            foreach (EntityQuery query in queries)
            {
                ComponentType[] componentTypes = query.GetReadWriteComponentTypes();
                foreach (ComponentType componentType in componentTypes)
                {
                    QueryComponentTypes.Add(componentType);
                }
            }

            QueryCount = queries.Length;
        }
    }
}