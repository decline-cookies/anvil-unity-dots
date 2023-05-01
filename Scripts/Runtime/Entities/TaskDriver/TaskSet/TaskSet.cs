using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    //TODO: #138 - Maybe we should have DriverTaskSet vs SystemTaskSet that extend AbstractTaskSet
    internal class TaskSet : AbstractAnvilBase
    {
        private readonly List<ICancellableDataStream> m_DataStreamsWithExplicitCancellation;
        private readonly Dictionary<Type, AbstractDataStream> m_PublicDataStreamsByType;
        private readonly Dictionary<Type, IMigratablePersistentData> m_MigratableEntityPersistentDataByType;

        private readonly List<AbstractJobConfig> m_JobConfigs;
        private readonly HashSet<Delegate> m_JobConfigSchedulingDelegates;

        private bool m_IsHardened;

        public CancelRequestsDataStream CancelRequestsDataStream { get; }
        public CancelProgressDataStream CancelProgressDataStream { get; }
        public CancelCompleteDataStream CancelCompleteDataStream { get; }

        public NativeArray<CancelRequestContext> CancelRequestsContexts { get; private set; }

        public ITaskSetOwner TaskSetOwner { get; }

        public int ExplicitCancellationCount
        {
            get => m_DataStreamsWithExplicitCancellation.Count;
        }


        public TaskSet(ITaskSetOwner taskSetOwner)
        {
            TaskSetOwner = taskSetOwner;
            m_JobConfigs = new List<AbstractJobConfig>();
            m_JobConfigSchedulingDelegates = new HashSet<Delegate>();

            m_DataStreamsWithExplicitCancellation = new List<ICancellableDataStream>();
            m_PublicDataStreamsByType = new Dictionary<Type, AbstractDataStream>();
            m_MigratableEntityPersistentDataByType = new Dictionary<Type, IMigratablePersistentData>();

            //TODO: #138 - Move all Cancellation aspects into one class to make it easier/nicer to work with

            CancelRequestsDataStream = new CancelRequestsDataStream(taskSetOwner);
            CancelCompleteDataStream = new CancelCompleteDataStream(taskSetOwner);
            CancelProgressDataStream = new CancelProgressDataStream(taskSetOwner);
        }

        protected override void DisposeSelf()
        {
            m_JobConfigs.DisposeAllAndTryClear();
            if (CancelRequestsContexts.IsCreated)
            {
                CancelRequestsContexts.Dispose();
            }
            m_MigratableEntityPersistentDataByType.DisposeAllValuesAndClear();

            base.DisposeSelf();
        }

        public void AddResolvableDataStreamsTo(Type type, List<AbstractDataStream> dataStreams)
        {
            if (!m_PublicDataStreamsByType.TryGetValue(type, out AbstractDataStream dataStream))
            {
                return;
            }

            dataStreams.Add(dataStream);
        }

        public EntityProxyDataStream<TInstance> GetOrCreateDataStream<TInstance>(CancelRequestBehaviour cancelRequestBehaviour)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            Type instanceType = typeof(TInstance);
            if (!m_PublicDataStreamsByType.TryGetValue(instanceType, out AbstractDataStream dataStream))
            {
                dataStream = CreateDataStream<TInstance>(cancelRequestBehaviour);
            }

            return (EntityProxyDataStream<TInstance>)dataStream;
        }

        public EntityProxyDataStream<TInstance> CreateDataStream<TInstance>(CancelRequestBehaviour cancelRequestBehaviour)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            EntityProxyDataStream<TInstance> dataStream = new EntityProxyDataStream<TInstance>(TaskSetOwner, cancelRequestBehaviour);
            switch (cancelRequestBehaviour)
            {
                case CancelRequestBehaviour.Delete:
                case CancelRequestBehaviour.Ignore:
                    break;

                case CancelRequestBehaviour.Unwind:
                    m_DataStreamsWithExplicitCancellation.Add(dataStream);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(cancelRequestBehaviour), cancelRequestBehaviour, null);
            }

            m_PublicDataStreamsByType.Add(typeof(TInstance), dataStream);

            return dataStream;
        }

        public EntityPersistentData<T> GetOrCreateEntityPersistentData<T>()
            where T : unmanaged, IEntityPersistentDataInstance
        {
            Type type = typeof(T);
            if (!m_MigratableEntityPersistentDataByType.TryGetValue(type, out IMigratablePersistentData persistentData))
            {
                persistentData = CreateEntityPersistentData<T>();
            }

            return (EntityPersistentData<T>)persistentData;
        }

        public EntityPersistentData<T> CreateEntityPersistentData<T>()
            where T : unmanaged, IEntityPersistentDataInstance
        {
            EntityPersistentData<T> entityPersistentData = new EntityPersistentData<T>();
            m_MigratableEntityPersistentDataByType.Add(typeof(T), entityPersistentData);
            return entityPersistentData;
        }

        public void AddJobConfigsTo(List<AbstractJobConfig> jobConfigs)
        {
            jobConfigs.AddRange(m_JobConfigs);
        }

        public IResolvableJobConfigRequirements ConfigureJobToUpdate<TInstance>(
            IAbstractDataStream<TInstance> dataStream,
            JobConfigScheduleDelegates.ScheduleUpdateJobDelegate<TInstance> scheduleJobFunction,
            BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            Debug_EnsureNoDuplicateJobSchedulingDelegates(scheduleJobFunction);

            UpdateJobConfig<TInstance> updateJobConfig
                = JobConfigFactory.CreateUpdateJobConfig(
                    TaskSetOwner,
                    (EntityProxyDataStream<TInstance>)dataStream,
                    scheduleJobFunction,
                    batchStrategy);
            m_JobConfigs.Add(updateJobConfig);

            return updateJobConfig;
        }

        public IResolvableJobConfigRequirements ConfigureJobToCancel<TInstance>(
            IAbstractDataStream<TInstance> pendingCancelDataStream,
            JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction,
            BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            Debug_EnsureNoDuplicateJobSchedulingDelegates(scheduleJobFunction);

            CancelJobConfig<TInstance> cancelJobConfig
                = JobConfigFactory.CreateCancelJobConfig(
                    TaskSetOwner,
                    (EntityProxyDataStream<TInstance>)pendingCancelDataStream,
                    scheduleJobFunction,
                    batchStrategy);
            m_JobConfigs.Add(cancelJobConfig);

            return cancelJobConfig;
        }

        public IJobConfig ConfigureJobTriggeredBy<TInstance>(
            IAbstractDataStream<TInstance> dataStream,
            JobConfigScheduleDelegates.ScheduleDataStreamJobDelegate<TInstance> scheduleJobFunction,
            BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            Debug_EnsureNoDuplicateJobSchedulingDelegates(scheduleJobFunction);

            DataStreamJobConfig<TInstance> dataStreamJobConfig
                = JobConfigFactory.CreateDataStreamJobConfig(
                    TaskSetOwner,
                    (EntityProxyDataStream<TInstance>)dataStream,
                    scheduleJobFunction,
                    batchStrategy);
            m_JobConfigs.Add(dataStreamJobConfig);

            return dataStreamJobConfig;
        }

        public IJobConfig ConfigureJobTriggeredBy(
            EntityQuery entityQuery,
            JobConfigScheduleDelegates.ScheduleEntityQueryJobDelegate scheduleJobFunction,
            BatchStrategy batchStrategy)
        {
            Debug_EnsureNoDuplicateJobSchedulingDelegates(scheduleJobFunction);

            EntityQueryJobConfig entityQueryJobConfig = JobConfigFactory.CreateEntityQueryJobConfig(
                TaskSetOwner,
                entityQuery,
                scheduleJobFunction,
                batchStrategy);
            m_JobConfigs.Add(entityQueryJobConfig);

            return entityQueryJobConfig;
        }

        public IJobConfig ConfigureJobTriggeredBy<T>(
            EntityQuery entityQuery,
            JobConfigScheduleDelegates.ScheduleEntityQueryComponentJobDelegate<T> scheduleJobFunction,
            BatchStrategy batchStrategy)
            where T : struct, IComponentData
        {
            Debug_EnsureNoDuplicateJobSchedulingDelegates(scheduleJobFunction);

            EntityQueryComponentJobConfig<T> entityQueryComponentJobConfig
                = JobConfigFactory.CreateEntityQueryComponentJobConfig(
                    TaskSetOwner,
                    entityQuery,
                    scheduleJobFunction,
                    batchStrategy);
            m_JobConfigs.Add(entityQueryComponentJobConfig);

            return entityQueryComponentJobConfig;
        }

        public void Harden()
        {
            Debug_EnsureNotHardened();
            m_IsHardened = true;

            foreach (AbstractJobConfig jobConfig in m_JobConfigs)
            {
                jobConfig.Harden();
            }

            //If we're a TaskDriver, we should harden our Cancel Requests
            if (TaskSetOwner != TaskSetOwner.TaskDriverSystem)
            {
                List<CancelRequestContext> contexts = new List<CancelRequestContext>();
                AddCancelRequestContextsTo(contexts);
                CancelRequestsContexts = new NativeArray<CancelRequestContext>(contexts.ToArray(), Allocator.Persistent);
            }
        }

        private void AddCancelRequestContextsTo(List<CancelRequestContext> contexts)
        {
            //Add ourself
            contexts.Add(new CancelRequestContext(TaskSetOwner.ID, CancelRequestsDataStream.ActiveID));

            //Add the System
            CancelRequestsDataStream systemCancelRequestsDataStream = TaskSetOwner.TaskDriverSystem.TaskSet.CancelRequestsDataStream;

            //We need to add a context for the System and the TaskDriver. When the System goes to update it's owned data, it doesn't know
            //all the different TaskDriver CancelRequests to read from. It only reads from its own CancelRequest collection.
            contexts.Add(new CancelRequestContext(systemCancelRequestsDataStream.TaskSetOwner.ID, systemCancelRequestsDataStream.ActiveID));
            contexts.Add(new CancelRequestContext(TaskSetOwner.ID, systemCancelRequestsDataStream.ActiveID));

            //Add all SubTask Drivers and their systems
            foreach (AbstractTaskDriver taskDriver in TaskSetOwner.SubTaskDrivers)
            {
                taskDriver.TaskSet.AddCancelRequestContextsTo(contexts);
            }
        }

        //*************************************************************************************************************
        // MIGRATION
        //*************************************************************************************************************

        public void AddToMigrationLookup(
            string parentPath,
            Dictionary<string, uint> migrationActiveIDLookup,
            PersistentDataSystem persistentDataSystem)
        {
            foreach (KeyValuePair<Type, AbstractDataStream> entry in m_PublicDataStreamsByType)
            {
                AddToMigrationLookup(parentPath, entry.Key.GetReadableName(), entry.Value.ActiveID, migrationActiveIDLookup);
            }

            foreach (ICancellableDataStream entry in m_DataStreamsWithExplicitCancellation)
            {
                AddToMigrationLookup(parentPath, $"{entry.InstanceType.GetReadableName()}-ExplicitCancel", entry.PendingCancelActiveID, migrationActiveIDLookup);
            }

            AddToMigrationLookup(
                parentPath,
                typeof(CancelRequestsDataStream).GetReadableName(),
                CancelRequestsDataStream.ActiveID,
                migrationActiveIDLookup);

            AddToMigrationLookup(
                parentPath,
                typeof(CancelProgressDataStream).GetReadableName(),
                CancelProgressDataStream.ActiveID,
                migrationActiveIDLookup);

            AddToMigrationLookup(
                parentPath,
                typeof(CancelCompleteDataStream).GetReadableName(),
                CancelCompleteDataStream.ActiveID,
                migrationActiveIDLookup);

            foreach (IMigratablePersistentData entry in m_MigratableEntityPersistentDataByType.Values)
            {
                persistentDataSystem.AddToMigrationLookup(parentPath, entry);
            }
        }

        private void AddToMigrationLookup(string parentPath, string name, uint activeID, Dictionary<string, uint> migrationActiveIDLookup)
        {
            string path = $"{parentPath}-{name}";
            Debug_EnsureNoDuplicateMigrationData(path, migrationActiveIDLookup);
            migrationActiveIDLookup.Add(path, activeID);
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNoDuplicateMigrationData(string path, Dictionary<string, uint> migrationActiveIDLookup)
        {
            if (migrationActiveIDLookup.ContainsKey(path))
            {
                throw new InvalidOperationException($"Trying to add ActiveID migration data for {this} but {path} is already in the lookup!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNotHardened()
        {
            if (m_IsHardened)
            {
                throw new InvalidOperationException($"Trying to Harden {this} but {nameof(Harden)} has already been called!");
            }
        }

        [Conditional("ANVIL_DEBUG_SAFETY_EXPENSIVE")]
        private void Debug_EnsureNoDuplicateJobSchedulingDelegates(Delegate jobSchedulingDelegate)
        {
            //TODO: #196 - Convert back to an error when configuration methods are made protected.
            // Until this issue is resolved there are valid uses cases where multiple task driver instances of the same
            // type configure on another task driver instance using the same schedule delegate.
            if (!m_JobConfigSchedulingDelegates.Add(jobSchedulingDelegate))
            {
                Logger.Warning($"Trying to add the delegate {jobSchedulingDelegate} but it is already being used. Although there are valid use cases this may indicate unintended duplicate processing of data. Investigate.");
            }
        }
    }
}
