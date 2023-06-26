using Anvil.CSharp.Collections;
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
                                                        IEntityWorldMigrationObserver,
                                                        IDataOwner
    {
        //TODO: #244 - Add in the ability to have Debug conversion from WorldUniqueID's to FixedStrings

        private readonly WorldDataOwnerLookup<DataOwnerID, ITaskSetOwner> m_TaskSetOwners;
        private readonly WorldDataOwnerLookup<DataTargetID, AbstractData> m_DataTargets;

        private readonly Dictionary<Type, IDataSource> m_EntityProxyDataSourcesByType;
        private readonly CancelRequestsDataSource m_CancelRequestsDataSource;
        private readonly CancelProgressDataSource m_CancelProgressDataSource;
        private readonly CancelCompleteDataSource m_CancelCompleteDataSource;
        private readonly List<CancelProgressFlow> m_CancelProgressFlows;
        private readonly Dictionary<Type, AccessController> m_UnityEntityDataAccessControllers;

        private bool m_IsHardened;
        private BulkJobScheduler<IDataSource> m_EntityProxyDataSourceBulkJobScheduler;
        private BulkJobScheduler<CancelProgressFlow> m_CancelProgressFlowBulkJobScheduler;
        private TaskDriverMigrationData m_TaskDriverMigrationData;

        public DataOwnerID WorldUniqueID { get; }

        public TaskDriverManagementSystem()
        {
            WorldUniqueID = GenerateWorldUniqueID();

            m_TaskSetOwners = new WorldDataOwnerLookup<DataOwnerID, ITaskSetOwner>();
            m_DataTargets = new WorldDataOwnerLookup<DataTargetID, AbstractData>();

            m_EntityProxyDataSourcesByType = new Dictionary<Type, IDataSource>();
            m_CancelRequestsDataSource = new CancelRequestsDataSource(this);
            m_CancelProgressDataSource = new CancelProgressDataSource(this);
            m_CancelCompleteDataSource = new CancelCompleteDataSource(this);
            m_CancelProgressFlows = new List<CancelProgressFlow>();
            m_UnityEntityDataAccessControllers = new Dictionary<Type, AccessController>();

            EntityKeyedTaskID.Debug_EnsureOffsetsAreCorrect();
            EntityWorldMigrationSystem.RegisterForEntityPatching<EntityKeyedTaskID>();
        }

        private DataOwnerID GenerateWorldUniqueID()
        {
            string idPath = $"{GetType().AssemblyQualifiedName}";
            return new DataOwnerID(idPath.GetBurstHashCode32());
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            EntityWorldMigrationSystem entityWorldMigrationSystem = World.GetOrCreateSystem<EntityWorldMigrationSystem>();
            entityWorldMigrationSystem.RegisterMigrationObserver(this);
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            if (m_IsHardened)
            {
                return;
            }
            Harden();
        }

        protected sealed override void OnDestroy()
        {
            m_EntityProxyDataSourcesByType.DisposeAllValuesAndClear();
            m_EntityProxyDataSourceBulkJobScheduler?.Dispose();
            m_CancelProgressFlowBulkJobScheduler?.Dispose();
            m_CancelProgressFlows.DisposeAllAndTryClear();
            m_UnityEntityDataAccessControllers.DisposeAllValuesAndClear();

            m_CancelRequestsDataSource.Dispose();
            m_CancelCompleteDataSource.Dispose();
            m_CancelProgressDataSource.Dispose();

            m_DataTargets.Dispose();

            m_TaskDriverMigrationData?.Dispose();


            base.OnDestroy();
        }

        private void Harden()
        {
            Debug_EnsureNotHardened();
            m_IsHardened = true;

            //For all the TaskDrivers, filter to find the ones that don't have Parents.
            //Those are our top level TaskDrivers
            List<AbstractTaskDriver> topLevelTaskDrivers = m_TaskSetOwners
                .Select(entry => entry.Value)
                .Where(entry => entry is AbstractTaskDriver)
                .Cast<AbstractTaskDriver>()
                .Where(taskDriver => taskDriver.Parent == null)
                .ToList();

            foreach (IDataSource dataSource in m_EntityProxyDataSourcesByType.Values)
            {
                dataSource.Harden();
            }

            m_EntityProxyDataSourceBulkJobScheduler = new BulkJobScheduler<IDataSource>(m_EntityProxyDataSourcesByType.Values.ToArray());

            //Then tell each top level Task Driver to Harden - This will Harden the associated sub task driver and the Task Driver System
            foreach (AbstractTaskDriver topLevelTaskDriver in topLevelTaskDrivers)
            {
                topLevelTaskDriver.Harden();
            }

            List<AbstractTaskDriverSystem> taskDriverSystems = m_TaskSetOwners
                .Select(entry => entry.Value)
                .Where(taskSetOwner => taskSetOwner is AbstractTaskDriverSystem)
                .Cast<AbstractTaskDriverSystem>()
                .ToList();

            //All the data has been hardened, we can Harden the Update Phase for the Systems
            foreach (AbstractTaskDriverSystem taskDriverSystem in taskDriverSystems)
            {
                taskDriverSystem.HardenUpdatePhase();
            }

            //Harden the Cancellation data
            m_CancelRequestsDataSource.Harden();
            m_CancelProgressDataSource.Harden();
            m_CancelCompleteDataSource.Harden();

            //Construct the CancelProgressFlows - Only create them if there is cancellable data
            m_CancelProgressFlows.AddRange(
                topLevelTaskDrivers.Where((topLevelTaskDriver) => ((ITaskSetOwner)topLevelTaskDriver).HasCancellableData)
                    .Select((topLevelTaskDriver) => new CancelProgressFlow(topLevelTaskDriver)));

            m_CancelProgressFlowBulkJobScheduler = new BulkJobScheduler<CancelProgressFlow>(m_CancelProgressFlows.ToArray());

            List<IDataSource> dataSources = new List<IDataSource>(m_EntityProxyDataSourcesByType.Values);
            dataSources.Add(m_CancelRequestsDataSource);
            dataSources.Add(m_CancelProgressDataSource);
            dataSources.Add(m_CancelCompleteDataSource);
            m_TaskDriverMigrationData = new TaskDriverMigrationData(dataSources);
        }

        //*************************************************************************************************************
        // INIT
        //*************************************************************************************************************

        public void RegisterTaskDriver(AbstractTaskDriver taskDriver)
        {
            Debug_EnsureNotHardened();
            //If we're a top level task driver, then this TaskDriverManagementSystem is our owner, otherwise it's our parent.
            m_TaskSetOwners.Add(taskDriver, taskDriver.Parent == null ? this : taskDriver.Parent);
            //All systems are owned by this
            m_TaskSetOwners.TryAdd(taskDriver.TaskDriverSystem, this);
        }

        public EntityProxyDataSource<TInstance> GetOrCreateEntityProxyDataSource<TInstance>()
            where TInstance : unmanaged, IEntityKeyedTask
        {
            Debug_EnsureNotHardened();
            Type type = typeof(TInstance);
            if (!m_EntityProxyDataSourcesByType.TryGetValue(type, out IDataSource dataSource))
            {
                dataSource = new EntityProxyDataSource<TInstance>(this);
                m_EntityProxyDataSourcesByType.Add(type, dataSource);
            }
            return (EntityProxyDataSource<TInstance>)dataSource;
        }

        public PendingData<T> CreatePendingData<T>(string uniqueContextIdentifier)
            where T : unmanaged, IEquatable<T>
        {
            Debug_EnsureNotHardened();
            return m_DataTargets.Create(CreatePendingDataInstance<T>, this, uniqueContextIdentifier);
        }

        private PendingData<T> CreatePendingDataInstance<T>(IDataOwner dataOwner, string uniqueContextIdentifier)
            where T : unmanaged, IEquatable<T>
        {
            return new PendingData<T>(dataOwner, uniqueContextIdentifier);
        }

        public ActiveArrayData<T> CreateActiveArrayData<T>(
            IDataOwner dataOwner,
            CancelRequestBehaviour cancelRequestBehaviour,
            AbstractData activeCancelData,
            string uniqueContextIdentifier)
            where T : unmanaged, IEquatable<T>
        {
            Debug_EnsureNotHardened();
            return m_DataTargets.Create(
                (createDataOwner, createUniqueContextIdentifier) => new ActiveArrayData<T>(
                    createDataOwner,
                    cancelRequestBehaviour,
                    activeCancelData,
                    createUniqueContextIdentifier),
                dataOwner,
                uniqueContextIdentifier);
        }

        public ActiveLookupData<T> CreateActiveLookupData<T>(
            IDataOwner dataOwner,
            CancelRequestBehaviour cancelRequestBehaviour,
            string uniqueContextIdentifier)
            where T : unmanaged, IEquatable<T>
        {
            Debug_EnsureNotHardened();
            return m_DataTargets.Create(
                (createDataOwner, createUniqueContextIdentifier) => new ActiveLookupData<T>(
                    createDataOwner,
                    cancelRequestBehaviour,
                    createUniqueContextIdentifier),
                dataOwner,
                uniqueContextIdentifier);
        }

        public bool TryGetActiveLookupDataByID<T>(DataTargetID dataTargetID, out ActiveLookupData<T> lookupData)
            where T : unmanaged, IEquatable<T>
        {
            bool doesExist = m_DataTargets.TryGetData(dataTargetID, out AbstractData data);
            lookupData = data as ActiveLookupData<T>;
            return doesExist;
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

            //Forcing a sync here so we can determine if we actually wrote anything. See AbstractData.IsDataInvalidated
            dependsOn.Complete();

            Dependency = dependsOn;
        }

        //*************************************************************************************************************
        // MIGRATION
        //*************************************************************************************************************

        JobHandle IEntityWorldMigrationObserver.MigrateTo(JobHandle dependsOn, World destinationWorld, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
        {
            TaskDriverManagementSystem destinationTaskDriverManagementSystem = destinationWorld.GetOrCreateSystem<TaskDriverManagementSystem>();
            Debug_EnsureOtherWorldTaskDriverManagementSystemExists(destinationWorld, destinationTaskDriverManagementSystem);

            return m_TaskDriverMigrationData.MigrateTo(
                dependsOn,
                destinationTaskDriverManagementSystem,
                destinationTaskDriverManagementSystem.m_TaskDriverMigrationData,
                ref remapArray);
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