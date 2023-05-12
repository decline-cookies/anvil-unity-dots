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

        private readonly List<AbstractJobConfig> m_JobConfigs;
        private readonly HashSet<Delegate> m_JobConfigSchedulingDelegates;
        private readonly PersistentDataSystem m_PersistentDataSystem;

        private readonly Dictionary<DataTargetID, AbstractDataStream> m_DataStreamLookupByID;
        private readonly Dictionary<Type, int> m_Debug_TypeCount;

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

            m_PersistentDataSystem = TaskSetOwner.World.GetOrCreateSystem<PersistentDataSystem>();

            //This is temporary until we refactor DataStreams and AbstractData as part of #241 
            m_DataStreamLookupByID = new Dictionary<DataTargetID, AbstractDataStream>();
            m_Debug_TypeCount = new Dictionary<Type, int>();

            m_JobConfigs = new List<AbstractJobConfig>();
            m_JobConfigSchedulingDelegates = new HashSet<Delegate>();

            m_DataStreamsWithExplicitCancellation = new List<ICancellableDataStream>();

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

            base.DisposeSelf();
        }

        public void AddResolvableDataStreamsTo(Type type, List<AbstractDataStream> dataStreams)
        {
            //TODO: This is temporary debug because we don't support more than one type of resolvable stream at the moment. #245 will fix this.
            if (m_Debug_TypeCount.TryGetValue(type, out int count) && count > 1)
            {
                throw new InvalidOperationException($"Trying to get resolvable data streams for the type {type.GetReadableName()} but there are more than one. We don't support this at this time!");
            }
            
            Type genericTypeArrayData = typeof(ActiveArrayData<>);
            Type genericTypeEntityProxyWrapperType = typeof(EntityProxyInstanceWrapper<>);

            Type specificEntityProxyWrapperType = genericTypeEntityProxyWrapperType.MakeGenericType(type);
            Type specificArrayType = genericTypeArrayData.MakeGenericType(specificEntityProxyWrapperType);
            
            //This is temporary and works on magic constant typing based on the data inside to get the same ID. #241 should fix this.
            DataTargetID targetID = AbstractData.GenerateWorldUniqueID(TaskSetOwner, specificArrayType, string.Empty);
            
            if (!m_DataStreamLookupByID.TryGetValue(targetID, out AbstractDataStream dataStream))
            {
                return;
            }

            dataStreams.Add(dataStream);
        }

        public EntityProxyDataStream<TInstance> GetOrCreateDataStream<TInstance>(CancelRequestBehaviour cancelRequestBehaviour, string uniqueContextIdentifier)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            //This is temporary and works on magic constant typing based on the data inside to get the same ID. #241 should fix this.
            DataTargetID targetID = AbstractData.GenerateWorldUniqueID(TaskSetOwner, typeof(ActiveArrayData<EntityProxyInstanceWrapper<TInstance>>), uniqueContextIdentifier);
            
            if (!m_DataStreamLookupByID.TryGetValue(targetID, out AbstractDataStream dataStream))
            {
                dataStream = CreateDataStream<TInstance>(cancelRequestBehaviour, uniqueContextIdentifier);
            }

            return (EntityProxyDataStream<TInstance>)dataStream;
        }

        public EntityProxyDataStream<TInstance> CreateDataStream<TInstance>(CancelRequestBehaviour cancelRequestBehaviour, string uniqueContextIdentifier)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            //This is temporary and works on magic constant typing based on the data inside to get the same ID. #241 should fix this.
            DataTargetID targetID = AbstractData.GenerateWorldUniqueID(TaskSetOwner, typeof(ActiveArrayData<EntityProxyInstanceWrapper<TInstance>>), uniqueContextIdentifier);

            
            EntityProxyDataStream<TInstance> dataStream = new EntityProxyDataStream<TInstance>(TaskSetOwner, cancelRequestBehaviour, uniqueContextIdentifier);
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

            
            m_DataStreamLookupByID.Add(targetID, dataStream);

            Type instanceType = typeof(TInstance);
            if (!m_Debug_TypeCount.TryGetValue(instanceType, out int count))
            {
                m_Debug_TypeCount[instanceType] = 0;
            }
            m_Debug_TypeCount[instanceType] = count + 1;

            return dataStream;
        }

        public EntityPersistentData<T> GetOrCreateEntityPersistentData<T>(string uniqueContextIdentifier)
            where T : unmanaged, IEntityPersistentDataInstance
        {
            return m_PersistentDataSystem.GetOrCreateEntityPersistentData<T>(TaskSetOwner, uniqueContextIdentifier);
        }

        public EntityPersistentData<T> CreateEntityPersistentData<T>(string uniqueContextIdentifier)
            where T : unmanaged, IEntityPersistentDataInstance
        {
            return m_PersistentDataSystem.CreateEntityPersistentData<T>(TaskSetOwner, uniqueContextIdentifier);
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
            contexts.Add(new CancelRequestContext(TaskSetOwner.WorldUniqueID, CancelRequestsDataStream.DataTargetID));

            //Add the System
            CancelRequestsDataStream systemCancelRequestsDataStream = TaskSetOwner.TaskDriverSystem.TaskSet.CancelRequestsDataStream;

            //We need to add a context for the System and the TaskDriver. When the System goes to update it's owned data, it doesn't know
            //all the different TaskDriver CancelRequests to read from. It only reads from its own CancelRequest collection.
            contexts.Add(new CancelRequestContext(systemCancelRequestsDataStream.TaskSetOwner.WorldUniqueID, systemCancelRequestsDataStream.DataTargetID));
            contexts.Add(new CancelRequestContext(TaskSetOwner.WorldUniqueID, systemCancelRequestsDataStream.DataTargetID));

            //Add all SubTask Drivers and their systems
            foreach (AbstractTaskDriver taskDriver in TaskSetOwner.SubTaskDrivers)
            {
                taskDriver.TaskSet.AddCancelRequestContextsTo(contexts);
            }
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ANVIL_DEBUG_SAFETY")]
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
