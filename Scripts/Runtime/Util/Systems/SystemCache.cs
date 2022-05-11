using System.Collections.Generic;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Util
{
    public class SystemCache : AbstractCache
    {
        public SystemGroupCache GroupCache
        {
            get;
            private set;
        }
        
        public ComponentSystemBase System
        {
            get;
        }

        public HashSet<ComponentType> QueryComponentTypes
        {
            get;
        } = new HashSet<ComponentType>();

        public int QueryCount
        {
            get;
            private set;
        }
        

        public SystemCache(SystemGroupCache groupCache, ComponentSystemBase system)
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
