using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Represents the cached state of a <see cref="World"/>
    /// </summary>
    public class WorldCache : AbstractCache
    {
        private static readonly Type COMPONENT_SYSTEM_GROUP_TYPE = typeof(ComponentSystemGroup);

        private readonly Dictionary<ComponentSystemBase, SystemCache> m_SystemCacheLookup = new Dictionary<ComponentSystemBase, SystemCache>();
        private readonly List<SystemCache> m_OrderedSystemCaches = new List<SystemCache>();
        private readonly List<SystemGroupCache> m_OrderedSystemGroupCaches = new List<SystemGroupCache>();

        private int m_LastRebuildCheckFrameCount;

        /// <summary>
        /// The <see cref="World"/> this represents.
        /// </summary>
        public World World
        {
            get;
        }

        internal WorldCache(World world)
        {
            World = world;
        }

        /// <summary>
        /// Given a set of <see cref="ComponentType"/>s, populate a list with all systems
        /// that have <see cref="EntityQuery"/>s that will write/read to any <see cref="ComponentType"/>
        /// in the set.
        /// </summary>
        /// <param name="componentTypes">Set of <see cref="ComponentType"/>s to find matching systems for.</param>
        /// <param name="matchingSystems">A list that will be populated with the matching systems</param>
        public void RefreshSystemsWithQueriesFor(HashSet<ComponentType> componentTypes, List<ComponentSystemBase> matchingSystems)
        {
            matchingSystems.Clear();

            foreach (SystemCache systemCache in m_OrderedSystemCaches)
            {
                foreach (ComponentType componentType in componentTypes)
                {
                    if (systemCache.QueryComponentTypes.Contains(componentType))
                    {
                        matchingSystems.Add(systemCache.System);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Forces a rebuild of the cache
        /// </summary>
        public void ForceRebuild()
        {
            m_LastRebuildCheckFrameCount = Time.frameCount;
            Rebuild();
        }

        /// <summary>
        /// Rebuilds the cache if needed.
        /// </summary>
        public void RebuildIfNeeded()
        {
            //This might be called many times a frame by many different callers.
            //We only want to do this check once per frame.
            int currentFrameCount = Time.frameCount;
            if (m_LastRebuildCheckFrameCount == currentFrameCount)
            {
                return;
            }

            m_LastRebuildCheckFrameCount = currentFrameCount;


            //If we've never been built before
            if (Version == INITIAL_VERSION)
            {
                Rebuild();
                return;
            }

            //If any of our groups have a different system count from the last time we built
            //Someone added a new system or removed a system
            //We can't tell if a new group was added or removed though
            foreach (SystemGroupCache groupSystemCache in m_OrderedSystemGroupCaches)
            {
                if (groupSystemCache.SystemGroup.Systems.Count == groupSystemCache.CachedGroupSystemsCount)
                {
                    continue;
                }

                Rebuild();
                return;
            }
        }

        private void Rebuild()
        {
            Version++;
            m_OrderedSystemCaches.Clear();
            m_OrderedSystemGroupCaches.Clear();
            //Because a system instance can be part of multiple groups, we need to clear the groups in case
            //we were removed from one of the groups. We'll get properly added to all groups in the rebuild process.
            foreach (SystemCache systemCache in m_SystemCacheLookup.Values)
            {
                systemCache.ClearGroups();
            }

            PlayerLoopSystem rootPlayerLoopSystem = PlayerLoop.GetCurrentPlayerLoop();
            ParsePlayerLoopSystem(ref rootPlayerLoopSystem);
        }

        private void ParsePlayerLoopSystem(ref PlayerLoopSystem playerLoopSystem)
        {
            if (playerLoopSystem.updateDelegate != null
             && COMPONENT_SYSTEM_GROUP_TYPE.IsAssignableFrom(playerLoopSystem.type)
             && PlayerLoopUtil.IsPlayerLoopSystemPartOfWorld(ref playerLoopSystem, World)
             && PlayerLoopUtil.TryGetSystemGroupFromPlayerLoopSystemNoChecks(ref playerLoopSystem, out ComponentSystemGroup group))
            {
                ParseSystemGroup(null, group);
            }

            //Phases won't have subsystems
            if (playerLoopSystem.subSystemList == null)
            {
                return;
            }

            //Loop through all children
            for (int i = 0; i < playerLoopSystem.subSystemList.Length; ++i)
            {
                ParsePlayerLoopSystem(ref playerLoopSystem.subSystemList[i]);
            }
        }

        private SystemCache GetOrCreateSystemCache(SystemGroupCache parentGroupCache, ComponentSystemBase system)
        {
            //See if we've ever created this SystemCache before and create if not
            if (!m_SystemCacheLookup.TryGetValue(system, out SystemCache systemCache))
            {
                if (system is ComponentSystemGroup group)
                {
                    systemCache = new SystemGroupCache(parentGroupCache, group);
                }
                else
                {
                    systemCache = new SystemCache(parentGroupCache, system);
                }

                m_SystemCacheLookup.Add(system, systemCache);
            }

            return systemCache;
        }

        private void ParseSystemGroup(SystemGroupCache parentGroupCache, ComponentSystemGroup group)
        {
            SystemGroupCache systemGroupCache = (SystemGroupCache)GetOrCreateSystemCache(parentGroupCache, group);
            //Ensure systems are sorted properly
            group.SortSystems();
            m_OrderedSystemGroupCaches.Add(systemGroupCache);
            m_OrderedSystemCaches.Add(systemGroupCache);
            systemGroupCache.RebuildIfNeeded(parentGroupCache);

            IReadOnlyList<ComponentSystemBase> groupSystems = systemGroupCache.SystemGroup.Systems;
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < groupSystems.Count; ++i)
            {
                ComponentSystemBase groupSystem = groupSystems[i];
                if (groupSystem is ComponentSystemGroup childGroup)
                {
                    ParseSystemGroup(systemGroupCache, childGroup);
                }
                else
                {
                    ParseSystem(systemGroupCache, groupSystem);
                }
            }
        }

        private void ParseSystem(SystemGroupCache parentGroupCache, ComponentSystemBase system)
        {
            SystemCache systemCache = GetOrCreateSystemCache(parentGroupCache, system);
            m_OrderedSystemCaches.Add(systemCache);
            systemCache.RebuildIfNeeded(parentGroupCache);
        }
    }
}
