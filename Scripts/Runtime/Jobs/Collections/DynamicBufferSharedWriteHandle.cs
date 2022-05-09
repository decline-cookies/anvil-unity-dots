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
        private readonly HashSet<ComponentSystemBase> m_ReadWriteSystems = new HashSet<ComponentSystemBase>();
        private readonly HashSet<ComponentSystemBase> m_SharedWriteSystems = new HashSet<ComponentSystemBase>();
        private readonly List<ComponentSystemBase> m_OrderedSystems = new List<ComponentSystemBase>();
        private readonly HashSet<ComponentType> m_QueryComponentTypes;
        private readonly World m_World;
        
        private ComponentSystemGroup m_ComponentSystemGroup;

        private JobHandle m_SharedWriteDependency;

        private int m_TotalNumberOfSystems;
        
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
            m_QueryComponentTypes = new HashSet<ComponentType>
            {
                ComponentType
            };
        }

        protected override void DisposeSelf()
        {
            // NOTE: If these asserts trigger we should think about calling Complete() on these job handles.
            Debug.Assert(m_SharedWriteDependency.IsCompleted, "The shared write access dependency is not completed");
            
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
            
            //TODO: Work through this logic
            int totalNumberOfSystems = m_World.Systems.Count;
            if (totalNumberOfSystems != m_TotalNumberOfSystems)
            {
                m_ComponentSystemGroup = callingSystem.UpdateComponentSystemGroup();
                SystemUtil.GetSystemsWithQueriesFor(m_World, m_ReadWriteSystems, m_QueryComponentTypes);
                SystemUtil.UpdateSystemOrderNonAlloc(m_ComponentSystemGroup, m_ReadWriteSystems, m_OrderedSystems);
                
                m_TotalNumberOfSystems = totalNumberOfSystems;
            }
            
            //TODO: Handle all system groups so we could get a sharedwrite even earlier
            //TODO: m_ComponentSystemGroup.AddSystemToUpdateList() or Remove
            
            //TODO: What happens if a systems query doesn't fill
            //TODO: Can we keep state for this iteration in a frame? Probably 
            
            ComponentSystemBase previousSystem = null;
            for (int i = 0; i < m_OrderedSystems.Count; ++i)
            {
                ComponentSystemBase currentSystem = m_OrderedSystems[i];
                
                //TODO: Check if system was disabled

                if (currentSystem != callingSystem)
                {
                    previousSystem = currentSystem;
                    continue;
                }

                if (!m_SharedWriteSystems.Contains(previousSystem))
                {
                    m_SharedWriteDependency = callingSystemDependency;
                }

                break;
            }

            return m_SharedWriteDependency;
        }
    }
}
