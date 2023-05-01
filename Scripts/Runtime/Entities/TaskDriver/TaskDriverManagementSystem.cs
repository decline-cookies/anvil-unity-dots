using Anvil.CSharp.Collections;
using Anvil.CSharp.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    //TODO: #108 - Custom Profiling -  https://github.com/decline-cookies/anvil-unity-dots/pull/111
    //TODO: #86 - Revisit with Entities 1.0 for "Create Before/After"
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    internal partial class TaskDriverManagementSystem : AbstractAnvilSystemBase,
                                                        IWorldMigrationObserver
    {
        private readonly Dictionary<Type, IDataSource> m_EntityProxyDataSourcesByType;
        private readonly HashSet<AbstractTaskDriver> m_AllTaskDrivers;
        private readonly HashSet<AbstractTaskDriverSystem> m_AllTaskDriverSystems;
        private readonly List<AbstractTaskDriver> m_TopLevelTaskDrivers;
        private readonly CancelRequestsDataSource m_CancelRequestsDataSource;
        private readonly CancelProgressDataSource m_CancelProgressDataSource;
        private readonly CancelCompleteDataSource m_CancelCompleteDataSource;
        private readonly List<CancelProgressFlow> m_CancelProgressFlows;
        private readonly Dictionary<Type, AccessController> m_UnityEntityDataAccessControllers;
        private readonly TaskDriverMigrationData m_TaskDriverMigrationData;
        
        
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
            m_CancelProgressDataSource = new CancelProgressDataSource(this);
            m_CancelCompleteDataSource = new CancelCompleteDataSource(this);
            m_CancelProgressFlows = new List<CancelProgressFlow>();
            m_UnityEntityDataAccessControllers = new Dictionary<Type, AccessController>();
            
            m_TaskDriverMigrationData = new TaskDriverMigrationData();
            m_TaskDriverMigrationData.AddDataSource(m_CancelRequestsDataSource);
            m_TaskDriverMigrationData.AddDataSource(m_CancelProgressDataSource);
            m_TaskDriverMigrationData.AddDataSource(m_CancelCompleteDataSource);
            
            EntityProxyInstanceID.Debug_EnsureOffsetsAreCorrect();
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            WorldEntityMigrationSystem worldEntityMigrationSystem = World.GetOrCreateSystem<WorldEntityMigrationSystem>();
            worldEntityMigrationSystem.RegisterMigrationObserver(this);
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
            m_CancelProgressFlows.DisposeAllAndTryClear();
            m_UnityEntityDataAccessControllers.DisposeAllValuesAndClear();
            m_TaskDriverMigrationData.Dispose();

            m_CancelRequestsDataSource.Dispose();
            m_CancelCompleteDataSource.Dispose();
            m_CancelProgressDataSource.Dispose();

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
            m_TopLevelTaskDrivers.AddRange(m_AllTaskDrivers.Where(taskDriver => taskDriver.Parent == null));

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
            m_CancelProgressDataSource.Harden();
            m_CancelCompleteDataSource.Harden();

            //Construct the CancelProgressFlows - Only create them if there is cancellable data
            m_CancelProgressFlows.AddRange(
                m_TopLevelTaskDrivers.Where((topLevelTaskDriver) => ((ITaskSetOwner)topLevelTaskDriver).HasCancellableData)
                    .Select((topLevelTaskDriver) => new CancelProgressFlow(topLevelTaskDriver)));

            m_CancelProgressFlowBulkJobScheduler = new BulkJobScheduler<CancelProgressFlow>(m_CancelProgressFlows.ToArray());
            
            //Build the Migration Data for this world
            m_TaskDriverMigrationData.PopulateMigrationLookup(World, m_TopLevelTaskDrivers);
        }

        public EntityProxyDataSource<TInstance> GetOrCreateEntityProxyDataSource<TInstance>()
            where TInstance : unmanaged, IEntityProxyInstance
        {
            Type type = typeof(TInstance);
            if (!m_EntityProxyDataSourcesByType.TryGetValue(type, out IDataSource dataSource))
            {
                dataSource = new EntityProxyDataSource<TInstance>(this);
                m_EntityProxyDataSourcesByType.Add(type, dataSource);
                m_TaskDriverMigrationData.AddDataSource(dataSource);
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

        public CancelProgressDataSource GetCancelProgressDataSource()
        {
            return m_CancelProgressDataSource;
        }

        public void RegisterTaskDriver(AbstractTaskDriver taskDriver)
        {
            Debug_EnsureNotHardened();
            m_AllTaskDrivers.Add(taskDriver);
            m_AllTaskDriverSystems.Add(taskDriver.TaskDriverSystem);
        }

        public AccessController GetOrCreateCDFEAccessController<T>()
            where T : struct, IComponentData
        {
            return GetOrCreateUnityEntityDataAccessController(typeof(T));
        }

        public AccessController GetOrCreateDBFEAccessController<T>()
            where T : struct, IBufferElementData
        {
            return GetOrCreateUnityEntityDataAccessController(typeof(T));
        }

        private AccessController GetOrCreateUnityEntityDataAccessController(Type type)
        {
            if (!m_UnityEntityDataAccessControllers.TryGetValue(type, out AccessController accessController))
            {
                accessController = new AccessController();
                m_UnityEntityDataAccessControllers.Add(type, accessController);
            }

            return accessController;
        }

        protected sealed override void OnUpdate()
        {
            JobHandle dependsOn = Dependency;

            //When someone has requested a cancel for a specific TaskDriver, that request is immediately propagated
            //down the entire chain to every Sub TaskDriver and their governing systems. So the first thing we need to
            //do is consolidate all the CancelRequestDataStreams so the lookups are all properly populated.
            dependsOn = m_CancelRequestsDataSource.Consolidate(dependsOn);

            //Next we check if any cancel progress was updated
            dependsOn = m_CancelProgressFlowBulkJobScheduler.Schedule(
                dependsOn,
                CancelProgressFlow.SCHEDULE_FUNCTION);


            //All Entity Proxy Data Streams will now be consolidated. Anything that was cancellable will be dealt with here as well
            //and written to the right location
            dependsOn = m_EntityProxyDataSourceBulkJobScheduler.Schedule(
                dependsOn,
                IDataSource.CONSOLIDATE_SCHEDULE_FUNCTION);

            // The Cancel Jobs will run later on in the frame and may have written that cancellation was completed to
            // the CancelCompletes. We'll consolidate those so cancels can propagate up the chain
            dependsOn = m_CancelCompleteDataSource.Consolidate(dependsOn);

            Dependency = dependsOn;
        }
        
        //*************************************************************************************************************
        // MIGRATION
        //*************************************************************************************************************

        JobHandle IWorldMigrationObserver.MigrateTo(JobHandle dependsOn, World destinationWorld, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
        {
            TaskDriverManagementSystem destinationTaskDriverManagementSystem = destinationWorld.GetOrCreateSystem<TaskDriverManagementSystem>();
            Debug_EnsureOtherWorldTaskDriverManagementSystemExists(destinationWorld, destinationTaskDriverManagementSystem);

            return m_TaskDriverMigrationData.MigrateTo(dependsOn, destinationWorld, destinationTaskDriverManagementSystem.m_TaskDriverMigrationData, ref remapArray);
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_EnsureNotHardened()
        {
            if (m_IsHardened)
            {
                throw new InvalidOperationException($"Expected {this} to not yet be Hardened but {nameof(Harden)} has already been called!");
            }
        }
        
        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_EnsureOtherWorldTaskDriverManagementSystemExists(World destinationWorld, TaskDriverManagementSystem taskDriverManagementSystem)
        {
            if (taskDriverManagementSystem == null)
            {
                throw new InvalidOperationException($"Expected World {destinationWorld} to have a {nameof(TaskDriverManagementSystem)} but it does not!");
            }
        }
    }
}
