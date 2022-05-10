using Anvil.CSharp.Core;
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine.LowLevel;

namespace Anvil.Unity.DOTS.Util
{
    public class WorldSystemState : AbstractAnvilBase
    {
        private class SystemGroupState
        {
            private int m_LastSystemGroupSystemCount;
            
            public ComponentSystemGroup SystemGroup
            {
                get;
            }
            
            public SystemGroupState(ComponentSystemGroup group)
            {
                SystemGroup = group;
                m_LastSystemGroupSystemCount = SystemGroup.Systems.Count;
            }
            
            public bool HasSystemCountInGroupChanged()
            {
                int currentCount = SystemGroup.Systems.Count;
                bool hasChanged = currentCount != m_LastSystemGroupSystemCount;
                m_LastSystemGroupSystemCount = currentCount;
                return hasChanged;
            }
        }
        
        private class SystemState
        {
            public ComponentSystemGroup SystemGroup
            {
                get;
            }

            public ComponentSystemBase System
            {
                get;
            }

            public HashSet<ComponentType> QueryComponentTypes
            {
                get;
            } = new HashSet<ComponentType>();

            
            public SystemState(ComponentSystemBase system, ComponentSystemGroup group)
            {
                System = system;
                SystemGroup = group;
            }
            
            

            public void RebuildQueryComponents()
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
            }
        }
        
        private static readonly Type s_ComponentSystemGroupType = typeof(ComponentSystemGroup);

        private readonly List<SystemState> m_OrderedSystemStates = new List<SystemState>();
        private readonly Dictionary<ComponentSystemBase, SystemState> m_SystemStateLookup = new Dictionary<ComponentSystemBase, SystemState>();
        private readonly List<ComponentSystemGroup> m_OrderedSystemGroups = new List<ComponentSystemGroup>();
        private readonly Dictionary<ComponentSystemGroup, SystemGroupState> m_SystemGroupStateLookup = new Dictionary<ComponentSystemGroup, SystemGroupState>();

        public World World
        {
            get;
        }

        public bool HasBuiltSystemStatesOnce
        {
            get;
            private set;
        }

        public WorldSystemState(World world)
        {
            World = world;
        }

        protected override void DisposeSelf()
        {
            //TODO: If we get disposed outside, we need to remove ourselves up the chain

            base.DisposeSelf();
        }

        public ComponentSystemGroup GetSystemGroupForSystem(ComponentSystemBase system)
        {
            if (!m_SystemStateLookup.TryGetValue(system, out SystemState systemState))
            {
                return null;
            }

            return systemState.SystemGroup;
        }

        public bool HasSystemCountInGroupChanged(ComponentSystemGroup systemGroup)
        {
            if (!m_SystemGroupStateLookup.TryGetValue(systemGroup, out SystemGroupState systemGroupState))
            {
                return true;
            }

            return systemGroupState.HasSystemCountInGroupChanged();
        }

        public void RebuildSystemStates()
        {
            m_OrderedSystemGroups.Clear();
            m_OrderedSystemStates.Clear();
            m_SystemStateLookup.Clear();
            m_SystemGroupStateLookup.Clear();
            PlayerLoopSystem rootPlayerLoopSystem = PlayerLoop.GetCurrentPlayerLoop();
            FindComponentSystemGroupsInPlayerLoop(rootPlayerLoopSystem);
            HasBuiltSystemStatesOnce = true;
        }
        
        private void FindComponentSystemGroupsInPlayerLoop(PlayerLoopSystem playerLoopSystem)
        {
            //Ordering the checks from least to most expensive
            //We need to have an updateDelegate in order to check if we're part of the World.
            //We then want to see if we're a group or not.
            //If we're a group then check if we're part of the World. If we are then all our systems are too.
            if (playerLoopSystem.updateDelegate != null 
             && s_ComponentSystemGroupType.IsAssignableFrom(playerLoopSystem.type)
             && PlayerLoopUtil.IsPlayerLoopSystemPartOfWorld(playerLoopSystem, World))
            {
                ComponentSystemGroup group = (ComponentSystemGroup)PlayerLoopUtil.GetSystemFromPlayerLoopSystem(playerLoopSystem);
                RebuildSystemStatesForGroup(group);
            }
            
            if (playerLoopSystem.subSystemList == null)
            {
                return;
            }

            foreach (PlayerLoopSystem childPlayerLoopSystem in playerLoopSystem.subSystemList)
            {
                FindComponentSystemGroupsInPlayerLoop(childPlayerLoopSystem);
            }
        }
        
        public void RebuildSystemStatesForGroup(ComponentSystemGroup group)
        {
            RebuildSystemGroupState(group);
            IReadOnlyList<ComponentSystemBase> groupSystems = group.Systems;
            foreach (ComponentSystemBase system in groupSystems)
            {
                if (system is ComponentSystemGroup systemGroup)
                {
                    RebuildSystemStatesForGroup(systemGroup);
                    continue;
                }
                RebuildSystemState(system, group);
            }
        }

        private void RebuildSystemGroupState(ComponentSystemGroup systemGroup)
        {
            if (!m_SystemGroupStateLookup.TryGetValue(systemGroup, out SystemGroupState systemGroupState))
            {
                systemGroupState = new SystemGroupState(systemGroup);
                m_SystemGroupStateLookup.Add(systemGroup, systemGroupState);
                m_OrderedSystemGroups.Add(systemGroup);
            }
        }

        private void RebuildSystemState(ComponentSystemBase system, ComponentSystemGroup group)
        {
            if (!m_SystemStateLookup.TryGetValue(system, out SystemState systemState))
            {
                systemState = new SystemState(system, group);
                m_SystemStateLookup.Add(system, systemState);
                m_OrderedSystemStates.Add(systemState);
            }

            systemState.RebuildQueryComponents();
        }

        public void RefreshSystemsWithQueriesFor(HashSet<ComponentType> componentTypes, List<ComponentSystemBase> matchingSystems)
        {
            matchingSystems.Clear();

            foreach (SystemState systemState in m_OrderedSystemStates)
            {
                foreach (ComponentType componentType in componentTypes)
                {
                    if (!systemState.QueryComponentTypes.Contains(componentType))
                    {
                        continue;
                    }
                    matchingSystems.Add(systemState.System);
                    break;
                }
            }
        }
    }
}
