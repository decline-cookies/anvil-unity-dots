using Anvil.CSharp.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    //TODO: #86 - Revisit with Entities 1.0 for "Create Before/After"
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    internal partial class TaskDriverManagementSystem : AbstractAnvilSystemBase
    {
        private static readonly Type ENTITY_PROXY_INSTANCE_ID_TYPE = typeof(EntityProxyInstanceID);

        private readonly Dictionary<Type, IDataSource> m_EntityProxyDataSourcesByType;
        private readonly HashSet<AbstractTaskDriver> m_AllTaskDrivers;
        private readonly HashSet<AbstractTaskDriverSystem> m_AllTaskDriverSystems;
        private readonly List<AbstractTaskDriver> m_TopLevelTaskDrivers;
        private readonly CancelRequestsDataSource m_CancelRequestsDataSource;

        private bool m_IsInitialized;
        private bool m_IsHardened;
        private BulkJobScheduler<IDataSource> m_EntityProxyDataSourceBulkJobScheduler;


        public TaskDriverManagementSystem()
        {
            m_EntityProxyDataSourcesByType = new Dictionary<Type, IDataSource>();
            m_AllTaskDrivers = new HashSet<AbstractTaskDriver>();
            m_AllTaskDriverSystems = new HashSet<AbstractTaskDriverSystem>();
            m_TopLevelTaskDrivers = new List<AbstractTaskDriver>();
            m_CancelRequestsDataSource = new CancelRequestsDataSource();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            if (m_IsInitialized)
            {
                return;
            }

            m_IsInitialized = true;

            Harden();
        }

        protected sealed override void OnDestroy()
        {
            m_EntityProxyDataSourcesByType.DisposeAllValuesAndClear();
            m_EntityProxyDataSourceBulkJobScheduler?.Dispose();
            
            m_CancelRequestsDataSource.Dispose();

            base.OnDestroy();
        }

        private void Harden()
        {
            Debug_EnsureNotHardened();
            m_IsHardened = true;

            foreach (IDataSource dataSource in m_EntityProxyDataSourcesByType.Values)
            {
                dataSource.Harden();
            }
            
            m_CancelRequestsDataSource.Harden();

            m_EntityProxyDataSourceBulkJobScheduler = new BulkJobScheduler<IDataSource>(m_EntityProxyDataSourcesByType.Values.ToArray());

            //For all the TaskDrivers, filter to find the ones that don't have Parents.
            //Those are our top level TaskDrivers
            foreach (AbstractTaskDriver taskDriver in m_AllTaskDrivers.Where(taskDriver => taskDriver.Parent == null))
            {
                m_TopLevelTaskDrivers.Add(taskDriver);
            }

            //Then tell each top level Task Driver to Harden - This will Harden the associated sub task driver and the Task Driver System
            foreach (AbstractTaskDriver topLevelTaskDriver in m_TopLevelTaskDrivers)
            {
                topLevelTaskDriver.Harden();
            }

            //All the data has been hardened, we can Harden the Update Phase for the Systems
            foreach (AbstractTaskDriverSystem taskDriverSystem in m_AllTaskDriverSystems)
            {
                taskDriverSystem.HardenUpdatePhase();
            }
        }

        public EntityProxyDataSource<TInstance> GetOrCreateEntityProxyDataSource<TInstance>()
            where TInstance : unmanaged, IEntityProxyInstance
        {
            Type type = typeof(TInstance);
            if (!m_EntityProxyDataSourcesByType.TryGetValue(type, out IDataSource dataSource))
            {
                dataSource = new EntityProxyDataSource<TInstance>();
                m_EntityProxyDataSourcesByType.Add(type, dataSource);
            }

            return (EntityProxyDataSource<TInstance>)dataSource;
        }

        public CancelRequestsDataSource GetCancelRequestsDataSource()
        {
            return m_CancelRequestsDataSource;
        }

        public void RegisterTaskDriver(AbstractTaskDriver taskDriver)
        {
            m_AllTaskDrivers.Add(taskDriver);
            m_AllTaskDriverSystems.Add(taskDriver.TaskDriverSystem);
        }

        protected sealed override void OnUpdate()
        {
            JobHandle dependsOn = Dependency;

            //TODO: Implement
            //When someone has requested a cancel for a specific TaskDriver, that request is immediately propagated
            //             //down the entire chain to every Sub TaskDriver and their governing systems. So the first thing we need to
            //             //do is consolidate all the CancelRequestDataStreams so the lookups are all properly populated.
            //             dependsOn = m_WorldCancelRequestsBulkJobScheduler.Schedule(dependsOn,
            //                                                                        AbstractDataStream.CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION);

            //Next we check if any cancel progress was updated
            //             dependsOn = m_WorldCancelProgressBulkJobScheduler.Schedule(dependsOn,
            //                                                                        TaskDriverCancelFlow.SCHEDULE_FUNCTION);


            dependsOn = m_EntityProxyDataSourceBulkJobScheduler.Schedule(dependsOn,
                                                                         IDataSource.CONSOLIDATE_SCHEDULE_FUNCTION);

            // Consolidate all PendingCancelDataStreams (Cancel jobs can run now)
            //             dependsOn = m_WorldPendingCancelBulkJobScheduler.Schedule(dependsOn,
            //                                                                       AbstractDataStream.CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION);

            // The Cancel Jobs will run later on in the frame and may have written that cancellation was completed to
            //             // the CancelCompletes. We'll consolidate those so cancels can propagate up the chain
            //             dependsOn = m_WorldCancelCompleteBulkJobScheduler.Schedule(dependsOn,
            //                                                                        AbstractDataStream.CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION);

            Dependency = dependsOn;
        }


        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNotHardened()
        {
            if (m_IsHardened)
            {
                throw new InvalidOperationException($"Trying to Harden {this} but {nameof(Harden)} has already been called!");
            }
        }
    }
}
