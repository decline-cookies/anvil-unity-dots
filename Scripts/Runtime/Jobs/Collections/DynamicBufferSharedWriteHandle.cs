using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Util;
using System;
using System.Collections.Generic;
using System.Reflection;
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
        private readonly HashSet<ComponentSystemBase> m_SharedWriteSystems = new HashSet<ComponentSystemBase>();
        private readonly List<ComponentSystemBase> m_OrderedSystems = new List<ComponentSystemBase>();
        private readonly Dictionary<ComponentSystemBase, int> m_OrderedSystemsLookup = new Dictionary<ComponentSystemBase, int>();
        private readonly HashSet<ComponentType> m_QueryComponentTypes;
        private readonly World m_World;
        private readonly WorldSystemState m_WorldSystemState;

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
            m_WorldSystemState = WorldSystemStateUtil.GetOrCreate(m_World);
            m_QueryComponentTypes = new HashSet<ComponentType>
            {
                ComponentType
            };
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

        private void RebuildWorldSystemState()
        {
            m_WorldSystemState.RebuildSystemStates();
            RebuildQueryOrders();
        }

        private void RebuildWorldSystemStateForCurrentGroup(ComponentSystemGroup systemGroup)
        {
            //TODO: What if we (callingSystem) are the one that changed groups?
            //TODO: Need to make sure this works as we expect
            m_WorldSystemState.RebuildSystemStatesForGroup(systemGroup);
            RebuildQueryOrders();
        }

        private void RebuildQueryOrders()
        {
            //We want to cache all the systems that deal with our component in the order they execute
            m_WorldSystemState.RefreshSystemsWithQueriesFor(m_QueryComponentTypes, m_OrderedSystems);
            
            //Build Lookup
            m_OrderedSystemsLookup.Clear();
            for (int i = 0; i < m_OrderedSystems.Count; ++i)
            {
                m_OrderedSystemsLookup[m_OrderedSystems[i]] = i;
            }
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

            //If this world state has never been built, we need to do a full build
            if (!m_WorldSystemState.HasBuiltSystemStatesOnce)
            {
                RebuildWorldSystemState();
            }

            ComponentSystemGroup callingGroup = m_WorldSystemState.GetSystemGroupForSystem(callingSystem);
            if (m_WorldSystemState.HasSystemCountInGroupChanged(callingGroup))
            {
                RebuildWorldSystemStateForCurrentGroup(callingGroup);
            }

            Debug.Assert(m_OrderedSystemsLookup.ContainsKey(callingSystem), $"{nameof(m_OrderedSystemsLookup)} does not contain {callingSystem}!");
            
            int callingSystemOrder = m_OrderedSystemsLookup[callingSystem];
            
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
                ComponentSystemBase previousSystem = m_OrderedSystems[callingSystemOrder - 1];
                
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
