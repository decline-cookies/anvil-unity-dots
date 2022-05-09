using Anvil.CSharp.Core;
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Util
{
    public static class SystemUtil
    {
        //*************************************************************************************************************
        // INTERNAL HELPER
        //*************************************************************************************************************
        
        private class WorldSystemUtil : AbstractAnvilBase
        {
            private readonly Dictionary<ComponentSystemBase, ComponentSystemGroup> m_ComponentSystemGroups = new Dictionary<ComponentSystemBase, ComponentSystemGroup>();

            internal World World
            {
                get;
            }

            private int m_TotalNumberOfSystems;

            public WorldSystemUtil(World world)
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

        private class WorldLookup : AbstractLookup<Type, World, WorldSystemUtil>
        {
            private static WorldSystemUtil CreationFunction(World world)
            {
                return new WorldSystemUtil(world);
            }
            
            public WorldLookup() : base(typeof(WorldLookup))
            {
            }

            public WorldSystemUtil GetOrCreate(World world)
            {
                return LookupGetOrCreate(world, CreationFunction);
            }
        }

        //*************************************************************************************************************
        // PUBLIC STATIC API
        //*************************************************************************************************************

        private static WorldLookup s_WorldLookup = new WorldLookup();
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Dispose();
            s_WorldLookup = new WorldLookup();
        }

        public static void Dispose()
        {
            s_WorldLookup.Dispose();
        }

        public static ComponentSystemGroup UpdateComponentSystemGroup(this ComponentSystemBase systemBase)
        {
            WorldSystemUtil worldSystemUtil = s_WorldLookup.GetOrCreate(systemBase.World);
            return worldSystemUtil.UpdateComponentSystemGroup(systemBase);
        }

        public static void GetSystemsWithQueriesFor(World world, HashSet<ComponentSystemBase> matchingSystems, HashSet<ComponentType> componentTypes)
        {
            WorldSystemUtil worldSystemUtil = s_WorldLookup.GetOrCreate(world);
            worldSystemUtil.GetSystemsWithQueriesFor(matchingSystems, componentTypes);
        }
        
        public static void UpdateSystemOrderNonAlloc(ComponentSystemGroup systemGroup, HashSet<ComponentSystemBase> systems, List<ComponentSystemBase> orderedSystems)
        {
            WorldSystemUtil worldSystemUtil = s_WorldLookup.GetOrCreate(systemGroup.World);
            worldSystemUtil.UpdateSystemOrderNonAlloc(systemGroup, systems, orderedSystems);
        }
    }
}
