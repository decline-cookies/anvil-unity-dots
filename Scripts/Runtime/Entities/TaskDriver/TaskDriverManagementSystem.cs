using Anvil.CSharp.Collections;
using Anvil.CSharp.Data;
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
        private readonly Dictionary<Type, IDataSource> m_EntityProxyDataSourcesByType;
        private readonly HashSet<AbstractTaskDriver> m_AllTaskDrivers;
        private readonly HashSet<AbstractTaskDriverSystem> m_AllTaskDriverSystems;
        private readonly List<AbstractTaskDriver> m_TopLevelTaskDrivers;
        private readonly CancelRequestsDataSource m_CancelRequestsDataSource;
        private readonly CancelCompleteDataSource m_CancelCompleteDataSource;
        private readonly List<CancelProgressFlow> m_CancelProgressFlows;

        private bool m_IsInitialized;
        private bool m_IsHardened;
        private BulkJobScheduler<IDataSource> m_EntityProxyDataSourceBulkJobScheduler;
        private BulkJobScheduler<CancelProgressFlow> m_CancelProgressFlowBulkJobScheduler;

        private readonly IDProvider m_IDProvider;
        


        public TaskDriverManagementSystem()
        {
            m_IDProvider = new IDProvider();
            m_EntityProxyDataSourcesByType = new Dictionary<Type, IDataSource>();
            m_AllTaskDrivers = new HashSet<AbstractTaskDriver>();
            m_AllTaskDriverSystems = new HashSet<AbstractTaskDriverSystem>();
            m_TopLevelTaskDrivers = new List<AbstractTaskDriver>();
            m_CancelRequestsDataSource = new CancelRequestsDataSource(this);
            m_CancelCompleteDataSource = new CancelCompleteDataSource(this);
            m_CancelProgressFlows = new List<CancelProgressFlow>();
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
            m_CancelProgressFlowBulkJobScheduler?.Dispose();
            
            m_CancelRequestsDataSource.Dispose();
            m_CancelCompleteDataSource.Dispose();
            
            m_IDProvider.Dispose();

            base.OnDestroy();
        }

        public uint GetNextID()
        {
            return m_IDProvider.GetNextID();
        }

        private void Harden()
        {
            Debug_EnsureNotHardened();
            m_IsHardened = true;

            foreach (IDataSource dataSource in m_EntityProxyDataSourcesByType.Values)
            {
                dataSource.Harden();
            }

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
            
            //Harden the Cancellation data
            m_CancelRequestsDataSource.Harden();
            m_CancelCompleteDataSource.Harden();
            
            //Construct the CancelProgressFlows
            foreach (AbstractTaskDriver topLevelTaskDriver in m_TopLevelTaskDrivers)
            {
                CancelProgressFlow cancelProgressFlow = new CancelProgressFlow(topLevelTaskDriver);
                m_CancelProgressFlows.Add(cancelProgressFlow);
            }

            m_CancelProgressFlowBulkJobScheduler = new BulkJobScheduler<CancelProgressFlow>(m_CancelProgressFlows.ToArray());

        }

        public EntityProxyDataSource<TInstance> GetOrCreateEntityProxyDataSource<TInstance>()
            where TInstance : unmanaged, IEntityProxyInstance
        {
            Type type = typeof(TInstance);
            if (!m_EntityProxyDataSourcesByType.TryGetValue(type, out IDataSource dataSource))
            {
                dataSource = new EntityProxyDataSource<TInstance>(this);
                m_EntityProxyDataSourcesByType.Add(type, dataSource);
            }

            return (EntityProxyDataSource<TInstance>)dataSource;
        }

        public CancelRequestsDataSource GetCancelRequestsDataSource()
        {
            return m_CancelRequestsDataSource;
        }

        public CancelCompleteDataSource GetCancelCompleteDataSource()
        {
            return m_CancelCompleteDataSource;
        }

        public void RegisterTaskDriver(AbstractTaskDriver taskDriver)
        {
            m_AllTaskDrivers.Add(taskDriver);
            m_AllTaskDriverSystems.Add(taskDriver.TaskDriverSystem);
        }

        protected sealed override void OnUpdate()
        {
            JobHandle dependsOn = Dependency;
            
            //When someone has requested a cancel for a specific TaskDriver, that request is immediately propagated
            //down the entire chain to every Sub TaskDriver and their governing systems. So the first thing we need to
            //do is consolidate all the CancelRequestDataStreams so the lookups are all properly populated.
            dependsOn = m_CancelRequestsDataSource.Consolidate(dependsOn);

            //Next we check if any cancel progress was updated
            dependsOn = m_CancelProgressFlowBulkJobScheduler.Schedule(dependsOn,
                                                                      CancelProgressFlow.SCHEDULE_FUNCTION);


            //All Entity Proxy Data Streams will now be consolidated. Anything that was cancellable will be dealt with here as well
            //and written to the right location
            dependsOn = m_EntityProxyDataSourceBulkJobScheduler.Schedule(dependsOn,
                                                                         IDataSource.CONSOLIDATE_SCHEDULE_FUNCTION);

            // The Cancel Jobs will run later on in the frame and may have written that cancellation was completed to
            // the CancelCompletes. We'll consolidate those so cancels can propagate up the chain
            dependsOn = m_CancelCompleteDataSource.Consolidate(dependsOn);

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
