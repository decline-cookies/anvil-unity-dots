using System.Collections.Generic;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Util
{
    /// <summary>
    /// Represents a cached state of a <see cref="ComponentSystemBase"/>
    /// </summary>
    public class SystemCache
    {
        /// <summary>
        /// The <see cref="SystemGroupCache"/> this is part of.
        /// </summary>
        public SystemGroupCache GroupCache
        {
            get;
            private set;
        }
        
        /// <summary>
        /// The <see cref="ComponentSystemBase"/> this cache represents
        /// </summary>
        public ComponentSystemBase System
        {
            get;
        }
        
        /// <summary>
        /// The <see cref="ComponentType"/>s used in any <see cref="EntityQuery"/>s
        /// on the <see cref="System"/>
        /// </summary>
        public HashSet<ComponentType> QueryComponentTypes
        {
            get;
        } = new HashSet<ComponentType>();
        
        /// <summary>
        /// The number of <see cref="EntityQuery"/>s this <see cref="System"/> has.
        /// </summary>
        public int QueryCount
        {
            get;
            private set;
        }
        
        internal SystemCache(SystemGroupCache groupCache, ComponentSystemBase system)
        {
            GroupCache = groupCache;
            System = system;
        }

        internal virtual void RebuildIfNeeded(SystemGroupCache parentGroupCache)
        {
            //No need to check if we're different, just assign
            GroupCache = parentGroupCache;
            
            if (QueryCount != System.EntityQueries.Length)
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
