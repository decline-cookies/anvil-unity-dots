using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Util;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// A utility class that handle managing access for shared writing (multiple job types writing at the same time)
    /// so that jobs can be scheduled easily.
    /// </summary>
    /// <remarks>
    /// This is similar to the <see cref="CollectionAccessController{TContext}"/> but for specific use with a
    /// <see cref="DynamicBuffer{T}"/> that other systems are also using for reading and/or writing but might
    /// not be aware of the access pattern for shared writing.
    /// <seealso cref="DynamicBufferSharedWriteUtil"/>
    /// </remarks>
    /// <typeparam name="T">The <see cref="IBufferElementData"/> type this instance is associated with.</typeparam>
    public class DynamicBufferSharedWriteHandle<T> : AbstractAnvilBase,
                                                     DynamicBufferSharedWriteUtil.IDynamicBufferSharedWriteHandle
        where T : IBufferElementData
    {
        private class LocalCache : AbstractCache
        {
            private readonly WorldCache m_WorldCache;
            private readonly HashSet<ComponentType> m_QueryComponentTypes;
            private readonly List<ComponentSystemBase> m_OrderedSystems = new List<ComponentSystemBase>();
            private readonly Dictionary<ComponentSystemBase, int> m_OrderedSystemsLookup = new Dictionary<ComponentSystemBase, int>();


            public LocalCache(WorldCache worldCache,
                              ComponentType componentType)
            {
                m_WorldCache = worldCache;
                m_QueryComponentTypes = new HashSet<ComponentType>
                {
                    componentType
                };
            }

            public int GetExecutionOrderOf(ComponentSystemBase callingSystem)
            {
                Debug.Assert(m_OrderedSystemsLookup.ContainsKey(callingSystem), $"{nameof(m_OrderedSystemsLookup)} does not contain {callingSystem}!");
                return m_OrderedSystemsLookup[callingSystem];
            }

            public ComponentSystemBase GetSystemAtExecutionOrder(int executionOrder)
            {
                Debug.Assert(executionOrder >= 0 && executionOrder < m_OrderedSystems.Count, $"Invalid execution order of {executionOrder}.{nameof(m_OrderedSystems)} Count is {m_OrderedSystems.Count}");
                return m_OrderedSystems[executionOrder];
            }

            public void RebuildIfNeeded()
            {
                //TODO: Frame check?
                
                //Rebuild the world cache if it needs to be
                m_WorldCache.RebuildIfNeeded();

                //If our local cache doesn't match the latest world cache, we need to update
                if (Version != m_WorldCache.Version)
                {
                    Rebuild();
                }
            }

            private void Rebuild()
            {
                m_WorldCache.RefreshSystemsWithQueriesFor(m_QueryComponentTypes, m_OrderedSystems);
                
                //Build Lookup
                m_OrderedSystemsLookup.Clear();
                for (int i = 0; i < m_OrderedSystems.Count; ++i)
                {
                    m_OrderedSystemsLookup[m_OrderedSystems[i]] = i;
                }
                Version++;
            }
        }
        
        
        private readonly HashSet<ComponentSystemBase> m_SharedWriteSystems = new HashSet<ComponentSystemBase>();
        
        private readonly World m_World;
        private readonly WorldCache m_WorldCache;
        private readonly LocalCache m_LocalCache;
        

        private JobHandle m_SharedWriteDependency;

        /// <summary>
        /// The <see cref="ComponentType"/> of <see cref="IBufferElementData"/> this instance is associated with.
        /// </summary>
        public ComponentType ComponentType
        {
            get;
        }

        internal DynamicBufferSharedWriteHandle(ComponentType type, World world)
        {
            ComponentType = type;
            m_World = world;
            m_WorldCache = WorldCacheUtil.GetOrCreate(m_World);
            m_LocalCache = new LocalCache(m_WorldCache, ComponentType);
            
        }

        protected override void DisposeSelf()
        {
            // NOTE: If these asserts trigger we should think about calling Complete() on these job handles.
            Debug.Assert(m_SharedWriteDependency.IsCompleted, "The shared write access dependency is not completed");
            
            //TODO: If we get disposed outside, we need to remove ourselves up the chain
            
            base.DisposeSelf();
        }
        
        /// <summary>
        /// Registers a <see cref="ComponentSystemBase"/> as a system that will shared write to
        /// the <see cref="DynamicBuffer{T}"/>.
        /// </summary>
        /// <param name="system">The <see cref="ComponentSystemBase"/> that shared writes.</param>
        public void RegisterSystemForSharedWrite(ComponentSystemBase system)
        {
            Debug.Assert(system.World == m_World, $"System {system} is not part of the same world as this {nameof(DynamicBufferSharedWriteHandle<T>)}");
            m_SharedWriteSystems.Add(system);
        }
        
        /// <summary>
        /// Unregisters a <see cref="ComponentSystemBase"/> as a system that will shared write to
        /// the <see cref="DynamicBuffer{T}"/>.
        /// </summary>
        /// <param name="system">The <see cref="ComponentSystemBase"/> that shared writes.</param>
        public void UnregisterSystemForSharedWrite(ComponentSystemBase system)
        {
            Debug.Assert(system.World == m_World, $"System {system} is not part of the same world as this {nameof(DynamicBufferSharedWriteHandle<T>)}");
            m_SharedWriteSystems.Remove(system);
        }
        
        /// <summary>
        /// Gets a <see cref="JobHandle"/> to be used to schedule the jobs that will shared writing to the
        /// <see cref="DynamicBuffer{T}"/>.
        /// </summary>
        /// <param name="callingSystem">The <see cref="SystemBase"/> that is doing the shared writing.</param>
        /// <param name="callingSystemDependency">The incoming Dependency <see cref="JobHandle"/> for the <paramref name="callingSystem"/></param>
        /// <returns></returns>
        public JobHandle GetSharedWriteJobHandle(SystemBase callingSystem, JobHandle callingSystemDependency)
        {
            Debug.Assert(m_SharedWriteSystems.Contains(callingSystem), $"Trying to get the shared write handle but {callingSystem} hasn't been registered. Did you call {nameof(RegisterSystemForSharedWrite)}?");
            
            //Rebuild our local cache if we need to. Will trigger a world cache rebuild if necessary too.
            m_LocalCache.RebuildIfNeeded();

            int callingSystemOrder = m_LocalCache.GetExecutionOrderOf(callingSystem);

            //If we're the first system to go in a frame, we're the first start point for shared writing.
            if (callingSystemOrder == 0)
            {
                m_SharedWriteDependency = callingSystemDependency;
            }
            else
            {
                //TODO: Check if system was disabled or query won't fill
                
                //Otherwise if the previous system is NOT a shared write system, then we need to move up
                //our dependency because the previous system did an exclusive write or shared read
                ComponentSystemBase previousSystem = m_LocalCache.GetSystemAtExecutionOrder(callingSystemOrder - 1);
                
                //We can check the versioning on the system to see if it was updated this frame
                
                if (!m_SharedWriteSystems.Contains(previousSystem))
                {
                    m_SharedWriteDependency = callingSystemDependency;
                }
            }
            
            return m_SharedWriteDependency;
        }
    }
}
