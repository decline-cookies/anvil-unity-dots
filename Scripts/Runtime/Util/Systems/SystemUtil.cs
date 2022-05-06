using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Util
{
    public static class SystemUtil
    {
        private static readonly Dictionary<World, SystemUtilImpl> s_SystemUtils = new Dictionary<World, SystemUtilImpl>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            s_SystemUtils.Clear();
        }

        public static ComponentSystemGroup UpdateComponentSystemGroup(this ComponentSystemBase systemBase)
        {
            SystemUtilImpl systemUtilImpl = GetOrCreateWorldInstance(systemBase.World);

            return systemUtilImpl.UpdateComponentSystemGroup(systemBase);
        }

        public static void GetSystemsWithQueriesFor(World world, HashSet<ComponentSystemBase> matchingSystems, HashSet<ComponentType> componentTypes)
        {
            SystemUtilImpl systemUtilImpl = GetOrCreateWorldInstance(world);
            systemUtilImpl.GetSystemsWithQueriesFor(matchingSystems, componentTypes);
        }
        
        public static void UpdateSystemOrderNonAlloc(ComponentSystemGroup systemGroup, HashSet<ComponentSystemBase> systems, List<ComponentSystemBase> orderedSystems)
        {
            SystemUtilImpl systemUtilImpl = GetOrCreateWorldInstance(systemGroup.World);
            systemUtilImpl.UpdateSystemOrderNonAlloc(systemGroup, systems, orderedSystems);
        }

        private static SystemUtilImpl GetOrCreateWorldInstance(World world)
        {
            if (!s_SystemUtils.TryGetValue(world, out SystemUtilImpl systemUtilImpl))
            {
                systemUtilImpl = new SystemUtilImpl(world);
                s_SystemUtils.Add(world, systemUtilImpl);
            }

            return systemUtilImpl;
        }
        
    }

    internal class SystemUtilImpl
    {
        private readonly Dictionary<ComponentSystemBase, ComponentSystemGroup> m_ComponentSystemGroups = new Dictionary<ComponentSystemBase, ComponentSystemGroup>();

        internal World World
        {
            get;
        }

        private int m_TotalNumberOfSystems;

        public SystemUtilImpl(World world)
        {
            World = world;
            RebuildSystemMappingToComponentSystemGroup();
        }

        private void RebuildSystemMappingToComponentSystemGroup()
        {
            m_ComponentSystemGroups.Clear();
            World.NoAllocReadOnlyCollection<ComponentSystemBase> allSystems = World.Systems;
            foreach (ComponentSystemBase system in allSystems)
            {
                if (!(system is ComponentSystemGroup systemGroup))
                {
                    continue;
                }
                    
                IReadOnlyList<ComponentSystemBase> groupSystems = systemGroup.Systems;
                foreach (ComponentSystemBase groupSystem in groupSystems)
                {
                    m_ComponentSystemGroups.Add(groupSystem, systemGroup);
                }
            }

            m_TotalNumberOfSystems = allSystems.Count;
        }
        
        internal ComponentSystemGroup UpdateComponentSystemGroup(ComponentSystemBase systemBase)
        {
            if (m_TotalNumberOfSystems != World.Systems.Count)
            {
                RebuildSystemMappingToComponentSystemGroup();
            }
            
            if (!m_ComponentSystemGroups.TryGetValue(systemBase, out ComponentSystemGroup componentSystemGroup))
            {
                throw new ArgumentException($"Tried to get {nameof(ComponentSystemGroup)} from lookup with {systemBase} but it doesn't exist in this World {World}!");
            }

            return componentSystemGroup;
        }

        internal void GetSystemsWithQueriesFor(HashSet<ComponentSystemBase> matchingSystems, HashSet<ComponentType> componentTypes)
        {
            matchingSystems.Clear();
            foreach (ComponentSystemBase system in m_ComponentSystemGroups.Keys)
            {
                EntityQuery[] queries = system.EntityQueries;
                
                foreach (EntityQuery query in queries)
                {
                    if (!query.ContainsAny(componentTypes))
                    {
                        continue;
                    }
                    matchingSystems.Add(system);
                }
            }
        }

        internal void UpdateSystemOrderNonAlloc(ComponentSystemGroup systemGroup, HashSet<ComponentSystemBase> systems, List<ComponentSystemBase> orderedSystems)
        {
            orderedSystems.Clear();
            IReadOnlyList<ComponentSystemBase> systemList = systemGroup.Systems;
            for (int i = 0; i < systemList.Count; ++i)
            {
                ComponentSystemBase candidateSystem = systemList[i];
                
                if (!systems.Contains(candidateSystem))
                {
                    continue;
                }

                orderedSystems.Add(candidateSystem);
            }
        }
    }
}
