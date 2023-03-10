using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine.UI;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    //TODO: #138 - Maybe we should have DriverTaskSet vs SystemTaskSet that extend AbstractTaskSet
    internal class TaskSet : AbstractAnvilBase
    {
        private readonly List<AbstractDataStream> m_DataStreamsWithExplicitCancellation;
        private readonly Dictionary<Type, AbstractDataStream> m_PublicDataStreamsByType;
        private readonly Dictionary<Type, AbstractPersistentData> m_EntityPersistentDataByType;

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

            m_DataStreamsWithExplicitCancellation = new List<AbstractDataStream>();
            m_PublicDataStreamsByType = new Dictionary<Type, AbstractDataStream>();
            m_EntityPersistentDataByType = new Dictionary<Type, AbstractPersistentData>();
            
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
            m_EntityPersistentDataByType.DisposeAllValuesAndClear();

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
            if (!m_EntityPersistentDataByType.TryGetValue(type, out AbstractPersistentData persistentData))
            {
                persistentData = CreateEntityPersistentData<T>();
                m_EntityPersistentDataByType.Add(type, persistentData);
            }

            return (EntityPersistentData<T>)persistentData;
        }

        public EntityPersistentData<T> CreateEntityPersistentData<T>()
            where T : unmanaged, IEntityPersistentDataInstance
        {
            EntityPersistentData<T> entityPersistentData = new EntityPersistentData<T>();
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

        public IJobConfig ConfigureJobWhenCancelComplete(
            in JobConfigScheduleDelegates.ScheduleDataStreamJobDelegate<CancelComplete> scheduleJobFunction,
            BatchStrategy batchStrategy)
        {
            Debug_EnsureNoDuplicateJobSchedulingDelegates(scheduleJobFunction);

            CancelCompleteJobConfig cancelCompleteJobConfig
                = JobConfigFactory.CreateCancelCompleteJobConfig(
                    TaskSetOwner,
                    CancelCompleteDataStream,
                    scheduleJobFunction,
                    batchStrategy);
            m_JobConfigs.Add(cancelCompleteJobConfig);
            return cancelCompleteJobConfig;
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


        public JobHandle AcquireCancelCompleteReaderAsync(out DataStreamActiveReader<CancelComplete> cancelCompleteReader)
        {
            JobHandle dependsOn = CancelCompleteDataStream.AcquireActiveAsync(AccessType.SharedRead);
            cancelCompleteReader = CancelCompleteDataStream.CreateDataStreamActiveReader();
            return dependsOn;
        }

        public void ReleaseCancelCompleteReaderAsync(JobHandle dependsOn)
        {
            CancelCompleteDataStream.ReleaseActiveAsync(dependsOn);
        }

        public DataStreamActiveReader<CancelComplete> AcquireCancelCompleteReader()
        {
            CancelCompleteDataStream.AcquireActive(AccessType.SharedRead);
            return CancelCompleteDataStream.CreateDataStreamActiveReader();
        }

        public void ReleaseCancelCompleteReader()
        {
            CancelCompleteDataStream.ReleaseActive();
        }

        public JobHandle AcquireCancelRequestsWriterAsync(out CancelRequestsWriter cancelRequestsWriter)
        {
            JobHandle dependsOn = CancelRequestsDataStream.AcquirePendingAsync(AccessType.SharedWrite);
            cancelRequestsWriter = CancelRequestsDataStream.CreateCancelRequestsWriter();
            return dependsOn;
        }

        public void ReleaseCancelRequestsWriterAsync(JobHandle dependsOn)
        {
            CancelRequestsDataStream.ReleasePendingAsync(dependsOn);
        }

        public CancelRequestsWriter AcquireCancelRequestsWriter()
        {
            CancelRequestsDataStream.AcquirePending(AccessType.SharedWrite);
            return CancelRequestsDataStream.CreateCancelRequestsWriter();
        }

        public void ReleaseCancelRequestsWriter()
        {
            CancelRequestsDataStream.ReleasePending();
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

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNoDuplicateJobSchedulingDelegates(Delegate jobSchedulingDelegate)
        {
            if (!m_JobConfigSchedulingDelegates.Add(jobSchedulingDelegate))
            {
                throw new InvalidOperationException($"Trying to add a delegate {jobSchedulingDelegate} but it is already being used!");
            }
        }
    }
}