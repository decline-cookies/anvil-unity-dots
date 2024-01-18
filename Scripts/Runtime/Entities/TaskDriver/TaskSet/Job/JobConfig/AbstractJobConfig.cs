using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal abstract class AbstractJobConfig : AbstractAnvilBase,
                                                IJobConfig
    {
        internal enum Usage
        {
            //Using in the general context (read or write)
            Default,

            //Using in an Updating context for data that can we resolved
            Update,

            //Using in the context where it can be written to via a resolve
            Resolve,

            //Using in the context for requesting a cancellation
            RequestCancel,

            //Using in the context for doing the work to cancel
            Cancelling
        }

        internal static readonly BulkScheduleDelegate<AbstractJobConfig> PREPARE_AND_SCHEDULE_FUNCTION
            = BulkSchedulingUtil.CreateSchedulingDelegate<AbstractJobConfig>(
                nameof(PrepareAndSchedule),
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly Usage[] USAGE_TYPES = (Usage[])Enum.GetValues(typeof(Usage));

        private readonly Dictionary<JobConfigDataID, AbstractAccessWrapper> m_AccessWrappers;
        private readonly List<AbstractAccessWrapper> m_SchedulingAccessWrappers;
        private readonly PersistentDataSystem m_PersistentDataSystem;

        private NativeArray<JobHandle> m_AccessWrapperDependencies;
        private AbstractScheduleInfo m_ScheduleInfo;
        private bool m_ShouldDisableAfterNextRun;
        private bool m_IsHardened;

        /// <inheritdoc cref="IJobConfig.IsEnabled"/>
        public bool IsEnabled
        {
            get;
            set;
        }

        internal ITaskSetOwner TaskSetOwner { get; }


        protected AbstractJobConfig(ITaskSetOwner taskSetOwner)
        {
            IsEnabled = true;
            TaskSetOwner = taskSetOwner;
            m_PersistentDataSystem = TaskSetOwner.World.GetOrCreateSystemManaged<PersistentDataSystem>();

            m_AccessWrappers = new Dictionary<JobConfigDataID, AbstractAccessWrapper>();
            m_SchedulingAccessWrappers = new List<AbstractAccessWrapper>();
        }

        internal void AssignScheduleInfo(AbstractScheduleInfo scheduleInfo)
        {
            Debug_EnsureNoScheduleInfo();
            m_ScheduleInfo = scheduleInfo;
        }

        protected override void DisposeSelf()
        {
            if (m_AccessWrapperDependencies.IsCreated)
            {
                m_AccessWrapperDependencies.Dispose();
            }

            m_AccessWrappers.DisposeAllValuesAndClear();

            base.DisposeSelf();
        }

        public override string ToString()
        {
            return $"{GetType().GetReadableName()} with schedule function name of {m_ScheduleInfo?.ScheduleJobFunctionInfo ?? "NOT YET SET"} on {TaskSetOwner}";
        }

        //*************************************************************************************************************
        // CONFIGURATION - COMMON
        //*************************************************************************************************************

        /// <inheritdoc cref="IJobConfig.RunOnce"/>
        public IJobConfig RunOnce()
        {
            IsEnabled = true;
            m_ShouldDisableAfterNextRun = true;

            return this;
        }

        protected void AddAccessWrapper(AbstractAccessWrapper accessWrapper)
        {
            Debug_EnsureWrapperUsage(accessWrapper);

            if (!m_AccessWrappers.TryAdd(accessWrapper.ID, accessWrapper))
            {
                ResolveDuplicateAccessWrappers(accessWrapper);
            }
        }

        private void ResolveDuplicateAccessWrappers(AbstractAccessWrapper accessWrapper)
        {
            AbstractAccessWrapper existingAccessWrapper = m_AccessWrappers[accessWrapper.ID];

            // If the existing access wrapper facilitates the needs of the new one then just keep the existing one.
            if (existingAccessWrapper.AccessType.IsCompatibleWith(accessWrapper.AccessType))
            {
                existingAccessWrapper.MergeStateFrom(accessWrapper);
                accessWrapper.Dispose();
            }
            // If the new access wrapper facilitates the needs of the existing one then dispose the existing wrapper and
            // use the new access wrapper.
            else if (accessWrapper.AccessType.IsCompatibleWith(existingAccessWrapper.AccessType))
            {
                accessWrapper.MergeStateFrom(existingAccessWrapper);
                existingAccessWrapper.Dispose();
                m_AccessWrappers[accessWrapper.ID] = accessWrapper;
            }
            // If there is no compatibility between the two requires error. The developer needs to fix this.
            else
            {
                throw new Exception($"There is no compatibility between ${nameof(AccessType)} requires on the same type. See previous message for details.");
            }

            //TODO: #112(anvil-unity-core) - Emit as a verbose warning
            // Logger.Warning($"Duplicate access requires resolved to {m_AccessWrappers[accessWrapper.ID].AccessType}. Existing:{existingAccessWrapper.AccessType}, Incoming:{accessWrapper.AccessType}"
            //     + $"\nThis is not necessarily a problem but could degrade scheduling performance less restrictive access was expected.");
        }


        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - Data Stream
        //*************************************************************************************************************

        /// <inheritdoc cref="IJobConfig.RequireDataStreamForWrite{TInstance}"/>
        public IJobConfig RequireDataStreamForWrite<TInstance>(IAbstractDataStream<TInstance> dataStream)
            where TInstance : unmanaged, IEntityKeyedTask
        {
            Debug_EnsureDataStreamContextWillBePreserved(dataStream);
            AddAccessWrapper(new DataStreamPendingAccessWrapper<TInstance>((EntityProxyDataStream<TInstance>)dataStream, AccessType.SharedWrite, Usage.Default));
            return this;
        }

        /// <inheritdoc cref="IJobConfig.RequireDataStreamForRead{TInstance}"/>
        public IJobConfig RequireDataStreamForRead<TInstance>(IAbstractDataStream<TInstance> dataStream)
            where TInstance : unmanaged, IEntityKeyedTask
        {
            Debug_EnsureDataStreamContextWillBePreserved(dataStream);
            AddAccessWrapper(new DataStreamActiveAccessWrapper<TInstance>((EntityProxyDataStream<TInstance>)dataStream, AccessType.SharedRead, Usage.Default));
            return this;
        }


        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - Generic Data
        //*************************************************************************************************************

        /// <inheritdoc cref="IJobConfig.RequireGenericDataForRead{TData}"/>
        public IJobConfig RequireGenericDataForRead<TData>(IReadAccessControlledValue<TData> collection)
            where TData : struct
        {
            AddAccessWrapper(new GenericDataReadOnlyAccessWrapper<TData>(collection, Usage.Default));
            return this;
        }

        /// <inheritdoc cref="IJobConfig.RequireGenericDataForSharedWrite{TData}"/>
        public IJobConfig RequireGenericDataForSharedWrite<TData>(ISharedWriteAccessControlledValue<TData> collection)
            where TData : struct
        {
            AddAccessWrapper(new GenericSharedWriteDataAccessWrapper<TData>(collection, Usage.Default));
            return this;
        }

        /// <inheritdoc cref="IJobConfig.RequireGenericDataForExclusiveWrite{TData}"/>
        public IJobConfig RequireGenericDataForExclusiveWrite<TData>(IExclusiveWriteAccessControlledValue<TData> collection)
            where TData : struct
        {
            AddAccessWrapper(new GenericExclusiveWriteDataAccessWrapper<TData>(collection, Usage.Default));
            return this;
        }


        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - Persistent Data
        //*************************************************************************************************************

        /// <inheritdoc cref="IJobConfig.RequireThreadPersistentDataForRead{TData}"/>
        public IJobConfig RequireThreadPersistentDataForRead<TData>(IThreadPersistentData<TData> threadPersistentData)
            where TData : unmanaged, IThreadPersistentDataInstance
        {
            AddAccessWrapper(new PersistentDataAccessWrapper<ThreadPersistentData<TData>>((ThreadPersistentData<TData>)threadPersistentData, AccessType.SharedRead, Usage.Default));

            return this;
        }

        /// <inheritdoc cref="IJobConfig.RequireThreadPersistentDataForWrite{TData}"/>
        public IJobConfig RequireThreadPersistentDataForWrite<TData>(IThreadPersistentData<TData> threadPersistentData)
            where TData : unmanaged, IThreadPersistentDataInstance
        {
            AddAccessWrapper(new PersistentDataAccessWrapper<ThreadPersistentData<TData>>((ThreadPersistentData<TData>)threadPersistentData, AccessType.SharedWrite, Usage.Default));

            return this;
        }

        /// <inheritdoc cref="IJobConfig.RequireEntityPersistentDataForRead{TData}"/>
        public IJobConfig RequireEntityPersistentDataForRead<TData>(IReadOnlyEntityPersistentData<TData> entityPersistentData)
            where TData : unmanaged, IEntityPersistentDataInstance
        {
            EntityPersistentData<TData> data = (EntityPersistentData<TData>)entityPersistentData;
            AddAccessWrapper(new PersistentDataAccessWrapper<EntityPersistentData<TData>>(data, AccessType.SharedRead, Usage.Default));

            return this;
        }

        /// <inheritdoc cref="IJobConfig.RequireEntityPersistentDataForSharedWrite{TData}"/>
        public IJobConfig RequireEntityPersistentDataForSharedWrite<TData>(IEntityPersistentData<TData> entityPersistentData)
            where TData : unmanaged, IEntityPersistentDataInstance
        {
            EntityPersistentData<TData> data = (EntityPersistentData<TData>)entityPersistentData;
            AddAccessWrapper(new PersistentDataAccessWrapper<EntityPersistentData<TData>>(data, AccessType.SharedWrite, Usage.Default));

            return this;
        }

        /// <inheritdoc cref="IJobConfig.RequireEntityPersistentDataForExclusiveWrite{TData}"/>
        public IJobConfig RequireEntityPersistentDataForExclusiveWrite<TData>(IEntityPersistentData<TData> entityPersistentData)
            where TData : unmanaged, IEntityPersistentDataInstance
        {
            EntityPersistentData<TData> data = (EntityPersistentData<TData>)entityPersistentData;
            AddAccessWrapper(new PersistentDataAccessWrapper<EntityPersistentData<TData>>(data, AccessType.ExclusiveWrite, Usage.Default));

            return this;
        }


        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - EntityQuery
        //*************************************************************************************************************

        /// <inheritdoc cref="IJobConfig.RequireEntityNativeArrayFromQueryForRead"/>
        public IJobConfig RequireEntityNativeArrayFromQueryForRead(EntityQuery entityQuery)
        {
            return RequireEntityNativeArrayFromQueryForRead(new EntityQueryNativeList(entityQuery));
        }

        /// <inheritdoc cref="IJobConfig.RequireIComponentDataNativeArrayFromQueryForRead"/>
        public IJobConfig RequireIComponentDataNativeArrayFromQueryForRead<T>(EntityQuery entityQuery)
            where T : unmanaged, IComponentData
        {
            return RequireIComponentDataNativeArrayFromQueryForRead(new EntityQueryComponentNativeList<T>(entityQuery));
        }

        protected IJobConfig RequireEntityNativeArrayFromQueryForRead(EntityQueryNativeList entityQueryNativeList)
        {
            AddAccessWrapper(new EntityQueryAccessWrapper(entityQueryNativeList, Usage.Default));
            return this;
        }

        protected IJobConfig RequireIComponentDataNativeArrayFromQueryForRead<T>(EntityQueryComponentNativeList<T> entityQueryNativeList)
            where T : unmanaged, IComponentData
        {
            AddAccessWrapper(new EntityQueryComponentAccessWrapper<T>(entityQueryNativeList, Usage.Default));
            return this;
        }


        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - Cancel
        //*************************************************************************************************************

        /// <inheritdoc cref="IJobConfig.RequestCancelFor"/>
        public IJobConfig RequestCancelFor(AbstractTaskDriver taskDriver)
        {
            AddAccessWrapper(new CancelRequestsPendingAccessWrapper(taskDriver.TaskSet.CancelRequestsDataStream, AccessType.SharedWrite, Usage.RequestCancel));
            return this;
        }


        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - ENTITY SPAWNER
        //*************************************************************************************************************

        /// <inheritdoc cref="IJobConfig.RequireEntitySpawner"/>
        public IJobConfig RequireEntitySpawner(EntitySpawnSystem entitySpawnSystem)
        {
            AddAccessWrapper(new EntitySpawnSystemWrapper(entitySpawnSystem, AccessType.SharedWrite, Usage.Default));
            return this;
        }


        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - ComponentLookup (CDFE)
        //*************************************************************************************************************

        //TODO: #86 - Revisit this section after Entities 1.0 upgrade for name changes to CDFE
        /// <inheritdoc cref="IJobConfig.RequireCDFEForRead{T}"/>
        public IJobConfig RequireCDFEForRead<T>() where T : unmanaged, IComponentData
        {
            AddAccessWrapper(new CDFEAccessWrapper<T>(AccessType.SharedRead, Usage.Default, TaskSetOwner.TaskDriverSystem));
            return this;
        }

        /// <inheritdoc cref="IJobConfig.RequireCDFEForSystemSharedWrite{T}"/>
        public IJobConfig RequireCDFEForSystemSharedWrite<T>() where T : unmanaged, IComponentData
        {
            AddAccessWrapper(new CDFEAccessWrapper<T>(AccessType.SharedWrite, Usage.Default, TaskSetOwner.TaskDriverSystem));
            return this;
        }

        /// <inheritdoc cref="IJobConfig.RequireCDFEForExclusiveWrite{T}"/>
        public IJobConfig RequireCDFEForExclusiveWrite<T>() where T : unmanaged, IComponentData
        {
            AddAccessWrapper(new CDFEAccessWrapper<T>(AccessType.ExclusiveWrite, Usage.Default, TaskSetOwner.TaskDriverSystem));
            return this;
        }


        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DynamicBuffer
        //*************************************************************************************************************

        /// <inheritdoc cref="IJobConfig.RequireDBFEForRead{T}"/>
        public IJobConfig RequireDBFEForRead<T>() where T : unmanaged, IBufferElementData
        {
            AddAccessWrapper(new DynamicBufferAccessWrapper<T>(AccessType.SharedRead, Usage.Default, TaskSetOwner.TaskDriverSystem));

            return this;
        }

        /// <inheritdoc cref="IJobConfig.RequireDBFEForSystemSharedWrite{T}"/>
        public IJobConfig RequireDBFEForSystemSharedWrite<T>() where T : unmanaged, IBufferElementData
        {
            AddAccessWrapper(new DynamicBufferAccessWrapper<T>(AccessType.SharedWrite, Usage.Default, TaskSetOwner.TaskDriverSystem));
            return this;
        }

        /// <inheritdoc cref="IJobConfig.RequireDBFEForExclusiveWrite{T}"/>
        public IJobConfig RequireDBFEForExclusiveWrite<T>() where T : unmanaged, IBufferElementData
        {
            AddAccessWrapper(new DynamicBufferAccessWrapper<T>(AccessType.ExclusiveWrite, Usage.Default, TaskSetOwner.TaskDriverSystem));
            return this;
        }


        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - EntityCommandBuffer
        //*************************************************************************************************************
        public IJobConfig RequireECB(EntityCommandBufferSystem ecbSystem)
        {
            AddAccessWrapper(new ECBAccessWrapper(ecbSystem, Usage.Default));

            return this;
        }


        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - External Requirements
        //*************************************************************************************************************

        /// <inheritdoc cref="IJobConfig.AddRequirementsFrom{T}"/>
        public IJobConfig AddRequirementsFrom<T>(T taskDriver, IJobConfig.ConfigureJobRequirementsDelegate<T> configureRequirements)
            where T : AbstractTaskDriver
        {
            return configureRequirements(taskDriver, this);
        }


        //*************************************************************************************************************
        // HARDEN
        //*************************************************************************************************************

        public void Harden()
        {
            //During Hardening we can optimize by pre-allocating native arrays for dependency combining and convert
            //dictionary iterations into lists. We also allow for sub classes to do their own optimizing if needed.

            Debug_EnsureNotHardened();
            m_IsHardened = true;

            HardenConfig();

            HashSet<Type> pendingAccessWrapperTypes = new HashSet<Type>();

            foreach (AbstractAccessWrapper wrapper in m_AccessWrappers.Values)
            {
                //Only allow one wrapper per type for DataStream Pending Access since they will all try to acquire/release
                //the same DataSource instance.
                if (wrapper is IDataStreamPendingAccessWrapper
                    && !pendingAccessWrapperTypes.Add(wrapper.ID.AccessWrapperType))
                {
                    continue;
                }

                m_SchedulingAccessWrappers.Add(wrapper);
            }

            m_AccessWrapperDependencies = new NativeArray<JobHandle>(m_SchedulingAccessWrappers.Count + 1, Allocator.Persistent);
        }

        protected virtual void HardenConfig() { }

        //*************************************************************************************************************
        // EXECUTION
        //*************************************************************************************************************

        private JobHandle PrepareAndSchedule(JobHandle dependsOn)
        {
            //The main use for JobConfig's, this handles getting the dependency for every piece of data that the job
            //will read from or write to and combine them into one to actually schedule the job with Unity's job
            //system. The resulting handle from that job is then fed back to each piece of data to allow Unity's
            //dependency system to know when it's safe to use the data again.

            Debug_EnsureScheduleInfoExists();

            if (!IsEnabled)
            {
                return dependsOn;
            }

            if (m_ShouldDisableAfterNextRun)
            {
                IsEnabled = false;
            }

            Debug_EnsureIsHardened();

            //If our scheduling data hasn't been modified, there's no reason to run the job
            if (!m_ScheduleInfo.ShouldSchedule())
            {
                return dependsOn;
            }

            int index = 0;
            for (; index < m_SchedulingAccessWrappers.Count; ++index)
            {
                m_AccessWrapperDependencies[index] = m_SchedulingAccessWrappers[index].AcquireAsync();
            }

            m_AccessWrapperDependencies[index] = dependsOn;

            dependsOn = JobHandle.CombineDependencies(m_AccessWrapperDependencies);
            dependsOn = m_ScheduleInfo.CallScheduleFunction(dependsOn);

            foreach (AbstractAccessWrapper wrapper in m_SchedulingAccessWrappers)
            {
                wrapper.ReleaseAsync(dependsOn);
            }

            return dependsOn;
        }

        private TWrapper GetAccessWrapper<TWrapper>(Usage usage)
            where TWrapper : AbstractAccessWrapper
        {
            JobConfigDataID id = new JobConfigDataID(typeof(TWrapper), usage);
            Debug_EnsureWrapperExists(id);

            return (TWrapper)m_AccessWrappers[id];
        }

        internal UnsafeParallelHashMap<EntityKeyedTaskID, bool> GetCancelProgressLookup()
        {
            CancelProgressLookupAccessWrapper cancelProgressLookupAccessWrapper
                = GetAccessWrapper<CancelProgressLookupAccessWrapper>(Usage.Cancelling);

            return cancelProgressLookupAccessWrapper.ProgressLookup;
        }

        internal CancelRequestsDataStream GetCancelRequestsDataStream(IAbstractCancelRequestDataStream explicitSource = null)
        {
            CancelRequestsPendingAccessWrapper cancelRequestsPendingAccessWrapper
                = GetAccessWrapper<CancelRequestsPendingAccessWrapper>(Usage.RequestCancel);

            return cancelRequestsPendingAccessWrapper.GetInstance(explicitSource);
        }

        internal EntityProxyDataStream<TInstance> GetPendingDataStream<TInstance>(
            Usage usage,
            IAbstractDataStream<TInstance> explicitSource = null)
            where TInstance : unmanaged, IEntityKeyedTask
        {
            DataStreamPendingAccessWrapper<TInstance> dataStreamAccessWrapper
                = GetAccessWrapper<DataStreamPendingAccessWrapper<TInstance>>(usage);

            return dataStreamAccessWrapper.GetInstance(explicitSource);
        }

        internal EntityProxyDataStream<TInstance> GetActiveDataStream<TInstance>(Usage usage)
            where TInstance : unmanaged, IEntityKeyedTask
        {
            DataStreamActiveAccessWrapper<TInstance> dataStreamAccessWrapper
                = GetAccessWrapper<DataStreamActiveAccessWrapper<TInstance>>(usage);

            return dataStreamAccessWrapper.DataStream;
        }

        internal EntityProxyDataStream<TInstance> GetActiveCancelDataStream<TInstance>()
            where TInstance : unmanaged, IEntityKeyedTask
        {
            DataStreamActiveCancelAccessWrapper<TInstance> dataStreamActiveCancelAccessWrapper
                = GetAccessWrapper<DataStreamActiveCancelAccessWrapper<TInstance>>(Usage.Cancelling);

            return dataStreamActiveCancelAccessWrapper.DataStream;
        }

        internal TData GetGenericDataForReading<TData>()
            where TData : struct
        {
            GenericDataReadOnlyAccessWrapper<TData> genericDataReadOnlyAccessWrapper
                = GetAccessWrapper<GenericDataReadOnlyAccessWrapper<TData>>(Usage.Default);

            return genericDataReadOnlyAccessWrapper.Data;
        }

        internal TData GetGenericDataForSharedWriting<TData>()
            where TData : struct
        {
            GenericSharedWriteDataAccessWrapper<TData> genericDataAccessWrapper
                = GetAccessWrapper<GenericSharedWriteDataAccessWrapper<TData>>(Usage.Default);

            return genericDataAccessWrapper.Data;
        }

        internal TData GetGenericDataForExclusiveWriting<TData>()
            where TData : struct
        {
            GenericExclusiveWriteDataAccessWrapper<TData> genericDataAccessWrapper
                = GetAccessWrapper<GenericExclusiveWriteDataAccessWrapper<TData>>(Usage.Default);

            return genericDataAccessWrapper.Data;
        }

        internal EntitySpawner GetEntitySpawner()
        {
            EntitySpawnSystemWrapper entitySpawnSystemWrapper
                = GetAccessWrapper<EntitySpawnSystemWrapper>(Usage.Default);

            return entitySpawnSystemWrapper.EntitySpawner;
        }

        internal void Fulfill<TData>(out ThreadPersistentData<TData> instance)
            where TData : unmanaged, IThreadPersistentDataInstance
        {
            PersistentDataAccessWrapper<ThreadPersistentData<TData>> persistentDataAccessWrapper
                = GetAccessWrapper<PersistentDataAccessWrapper<ThreadPersistentData<TData>>>(Usage.Default);

            instance = persistentDataAccessWrapper.PersistentData;
        }

        internal void Fulfill<TData>(out EntityPersistentData<TData> instance)
            where TData : unmanaged, IEntityPersistentDataInstance
        {
            PersistentDataAccessWrapper<EntityPersistentData<TData>> persistentDataAccessWrapper
                = GetAccessWrapper<PersistentDataAccessWrapper<EntityPersistentData<TData>>>(Usage.Default);

            instance = persistentDataAccessWrapper.PersistentData;
        }

        internal NativeList<Entity> GetEntityNativeListFromQuery()
        {
            EntityQueryAccessWrapper entityQueryAccessWrapper
                = GetAccessWrapper<EntityQueryAccessWrapper>(Usage.Default);

            return entityQueryAccessWrapper.NativeList;
        }

        internal NativeList<T> GetIComponentDataNativeListFromQuery<T>()
            where T : unmanaged, IComponentData
        {
            EntityQueryComponentAccessWrapper<T> entityQueryAccessWrapper
                = GetAccessWrapper<EntityQueryComponentAccessWrapper<T>>(Usage.Default);

            return entityQueryAccessWrapper.NativeList;
        }

        internal void Fulfill<T>(out CDFEReader<T> instance)
            where T : unmanaged, IComponentData
        {
            CDFEAccessWrapper<T> cdfeAccessWrapper = GetAccessWrapper<CDFEAccessWrapper<T>>(Usage.Default);
            instance = cdfeAccessWrapper.CreateCDFEReader();
        }

        internal void Fulfill<T>(out CDFEWriter<T> instance)
            where T : unmanaged, IComponentData
        {
            CDFEAccessWrapper<T> cdfeAccessWrapper = GetAccessWrapper<CDFEAccessWrapper<T>>(Usage.Default);
            instance = cdfeAccessWrapper.CreateCDFEUpdater();
        }

        internal void Fulfill<T>(out DBFEForRead<T> instance)
            where T : unmanaged, IBufferElementData
        {
            DynamicBufferAccessWrapper<T> dynamicBufferAccessWrapper = GetAccessWrapper<DynamicBufferAccessWrapper<T>>(Usage.Default);
            instance = dynamicBufferAccessWrapper.CreateDynamicBufferReader();
        }

        internal void Fulfill<T>(out DBFEForExclusiveWrite<T> instance)
            where T : unmanaged, IBufferElementData
        {
            DynamicBufferAccessWrapper<T> dynamicBufferAccessWrapper = GetAccessWrapper<DynamicBufferAccessWrapper<T>>(Usage.Default);
            instance = dynamicBufferAccessWrapper.CreateDynamicBufferExclusiveWriter();
        }

        internal void Fulfill(out EntityCommandBuffer instance)
        {
            ECBAccessWrapper ecbAccessWrapper = GetAccessWrapper<ECBAccessWrapper>(Usage.Default);
            instance = ecbAccessWrapper.CommandBuffer;
        }

        internal void Fulfill(out EntityCommandBuffer.ParallelWriter instance)
        {
            ECBAccessWrapper ecbAccessWrapper = GetAccessWrapper<ECBAccessWrapper>(Usage.Default);
            instance = ecbAccessWrapper.CommandBuffer.AsParallelWriter();
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_EnsureIsHardened()
        {
            if (m_IsHardened == false)
            {
                throw new InvalidOperationException($"{this} is not hardened yet!");
            }
        }

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_EnsureNotHardened()
        {
            if (m_IsHardened == true)
            {
                throw new InvalidOperationException($"{this} is already hardened!");
            }
        }

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_EnsureWrapperExists(JobConfigDataID id)
        {
            if (!m_AccessWrappers.ContainsKey(id))
            {
                throw new InvalidOperationException($"Job configured by {this} tried to access {id.AccessWrapperType.GetReadableName()} data for {id.Usage} but it wasn't found. Did you call the right Require function?");
            }
        }

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_EnsureWrapperUsage(AbstractAccessWrapper wrapper)
        {
            if (wrapper.Debug_WrapperType != typeof(AbstractDataStreamActiveAccessWrapper<>))
            {
                return;
            }

            //TODO: #140 - Detect common configuration issues and let the developer know

            // //Access checks
            // switch (wrapper.ID.Usage)
            // {
            //     case Usage.Update:
            //         //While updating, the same type could be cancelling.
            //         Debug_EnsureWrapperUsageValid(wrapper.ID, Usage.WritePendingCancel);
            //         break;
            //     case Usage.Write:
            //         //Allowed to read while writing because we are writing to UnsafeTypedStream and reading from NativeArray
            //         Debug_EnsureWrapperUsageValid(wrapper.ID, Usage.Read);
            //         break;
            //     case Usage.Read:
            //         //Allowed to write while reading because we are writing to UnsafeTypedStream and reading from NativeArray
            //         Debug_EnsureWrapperUsageValid(wrapper.ID, Usage.Write);
            //         break;
            //     case Usage.WritePendingCancel:
            //         //We'll be updating when writing to cancel
            //         Debug_EnsureWrapperUsageValid(wrapper.ID, Usage.Update);
            //         break;
            //     case Usage.Cancelling:
            //         //When we're cancelling, we can read or write to others because we're operating on a different stream
            //         Debug_EnsureWrapperUsageValid(wrapper.ID, Usage.Read, Usage.Write);
            //         break;
            //     default:
            //         throw new ArgumentOutOfRangeException($"Trying to switch on {nameof(wrapper.ID.Usage)} but no code path satisfies for {wrapper.ID.Usage}!");
            // }
        }

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_EnsureWrapperUsageValid(JobConfigDataID id, params Usage[] allowedUsages)
        {
            foreach (Usage usage in USAGE_TYPES)
            {
                //Don't check against ourself or any of the allowed usages
                if (id.Usage == usage || allowedUsages.Contains(usage))
                {
                    continue;
                }

                JobConfigDataID checkID = new JobConfigDataID(id.AccessWrapperType, usage);
                if (m_AccessWrappers.ContainsKey(checkID))
                {
                    throw new InvalidOperationException($"{this} is trying to require {id.AccessWrapperType.GetReadableName()} data for {id.Usage} but the same type is being used for {usage} which is not allowed!");
                }
            }
        }

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_EnsureNoScheduleInfo()
        {
            if (m_ScheduleInfo != null)
            {
                throw new InvalidOperationException($"{this} is trying to schedule a job but it already has Schedule Info {m_ScheduleInfo} defined! Only schedule one piece of data!");
            }
        }


        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_EnsureScheduleInfoExists()
        {
            if (m_ScheduleInfo == null)
            {
                throw new InvalidOperationException($"{this} does not have a {nameof(AbstractScheduleInfo)} yet! Please schedule on some data first.");
            }
        }

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_EnsureDataStreamContextWillBePreserved<TInstance>(IAbstractDataStream<TInstance> dataStream)
            where TInstance : unmanaged, IEntityKeyedTask
        {
            if (TaskSetOwner.TaskDriverSystem == TaskSetOwner && dataStream is IDriverDataStream<TInstance>)
            {
                throw new InvalidOperationException($"{this} is a system job that is trying to write to a {nameof(IDriverDataStream<TInstance>)} data stream. If there are more than one TaskDriver, this job will always only write to the first TaskDriver instance. Using {nameof(IResolvableJobConfigRequirements.RequireResolveTarget)} to properly pipe results to the correct TaskDriver instance.");
            }
        }
    }
}