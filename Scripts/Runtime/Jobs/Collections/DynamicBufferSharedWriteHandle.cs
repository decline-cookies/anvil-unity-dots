using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Util;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Anvil.Unity.DOTS.Jobs
{
    public class DynamicBufferSharedWriteHandle<T> : AbstractAnvilBase,
                                                     DynamicBufferSharedWriteUtil.IDynamicBufferSharedWriteHandle
        where T : IBufferElementData
    {
        private readonly HashSet<ComponentSystemBase> m_ReadWriteSystems = new HashSet<ComponentSystemBase>();
        private readonly HashSet<ComponentSystemBase> m_SharedWriteSystems = new HashSet<ComponentSystemBase>();
        private readonly List<ComponentSystemBase> m_OrderedSystems = new List<ComponentSystemBase>();
        private readonly HashSet<ComponentType> m_ComponentTypes;
        private readonly World m_World;
        
        private ComponentSystemGroup m_ComponentSystemGroup;

        private JobHandle m_SharedWriteDependency;

        private int m_TotalNumberOfSystems;

        public ComponentType ComponentType
        {
            get;
        }

        internal DynamicBufferSharedWriteHandle(ComponentType type, World world)
        {
            ComponentType = type;
            m_World = world;
            m_ComponentTypes = new HashSet<ComponentType>
            {
                ComponentType.ReadWrite<T>(),
                ComponentType.ReadOnly<T>()
            };
        }

        protected override void DisposeSelf()
        {
            base.DisposeSelf();
        }

        public void RegisterSystemForSharedWrite(ComponentSystemBase system)
        {
            Debug.Assert(system.World == m_World, $"System {system} is not part of the same world as this {nameof(DynamicBufferSharedWriteHandle<T>)}");
            m_SharedWriteSystems.Add(system);
        }

        public JobHandle GetSharedWriteJobHandle(SystemBase callingSystem, JobHandle callingSystemDependency)
        {
            Debug.Assert(m_SharedWriteSystems.Contains(callingSystem), $"Trying to get the shared write handle but {callingSystem} hasn't been registered.");

            int totalNumberOfSystems = m_World.Systems.Count;
            if (totalNumberOfSystems != m_TotalNumberOfSystems)
            {
                m_ComponentSystemGroup = callingSystem.UpdateComponentSystemGroup();
                SystemUtil.GetSystemsWithQueriesFor(m_World, m_ReadWriteSystems, m_ComponentTypes);
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
