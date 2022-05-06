using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Util
{
    public static class EntityQueryExtensions
    {
        private static readonly MethodInfo s_QueryGetReadAndWriteTypesMethodInfo = typeof(EntityQuery).GetMethod("GetReadAndWriteTypes", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly Dictionary<EntityQuery, HashSet<ComponentType>> m_EntityQueryComponentsLookup = new Dictionary<EntityQuery, HashSet<ComponentType>>();


        public static HashSet<ComponentType> GetAllComponentTypesForQuery(this EntityQuery entityQuery)
        {
            if (!m_EntityQueryComponentsLookup.TryGetValue(entityQuery, out HashSet<ComponentType> queryTypes))
            {
                ComponentType[] componentTypes = (ComponentType[])s_QueryGetReadAndWriteTypesMethodInfo.Invoke(entityQuery, null);
                queryTypes = new HashSet<ComponentType>();
                foreach (ComponentType componentType in componentTypes)
                {
                    queryTypes.Add(componentType);
                }
                m_EntityQueryComponentsLookup.Add(entityQuery, queryTypes);
            }

            return queryTypes;
        }
        
        
        public static bool ContainsAny(this EntityQuery entityQuery, HashSet<ComponentType> typesToCheck)
        {
            HashSet<ComponentType> queryTypes = entityQuery.GetAllComponentTypesForQuery();

            foreach (ComponentType componentType in typesToCheck)
            {
                if (queryTypes.Contains(componentType))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
