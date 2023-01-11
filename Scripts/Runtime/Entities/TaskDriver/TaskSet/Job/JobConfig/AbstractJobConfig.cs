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
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractJobConfig : AbstractAnvilBase,
                                                IJobConfig
    {
        //TODO: Change to better description
        internal enum Usage
        {
            Default,
            Update,
            Resolve,
            RequestCancel,
            CancelComplete,
            Cancelling
        }

        internal static readonly BulkScheduleDelegate<AbstractJobConfig> PREPARE_AND_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<AbstractJobConfig>(nameof(PrepareAndSchedule), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly Usage[] USAGE_TYPES = (Usage[])Enum.GetValues(typeof(Usage));

        private readonly string m_TypeString;
        private readonly Dictionary<JobConfigDataID, AbstractAccessWrapper> m_AccessWrappers;
        private readonly List<AbstractAccessWrapper> m_SchedulingAccessWrappers;

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
            m_ShouldDisableAfterNextRun = true;
            return this;
        }

        protected void AddAccessWrapper(AbstractAccessWrapper accessWrapper)
        {
            Debug_EnsureWrapperValidity(accessWrapper.ID);
            Debug_EnsureWrapperUsage(accessWrapper);
            m_AccessWrappers.Add(accessWrapper.ID, accessWrapper);
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DATA STREAM
        //*************************************************************************************************************
        
        public IJobConfig RequireDataStreamForWrite<TInstance>(IAbstractDataStream<TInstance> dataStream)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            AddAccessWrapper(new DataStreamPendingAccessWrapper<TInstance>((EntityProxyDataStream<TInstance>)dataStream, AccessType.SharedWrite, Usage.Default));
            return this;
        }

        public IJobConfig RequireDataStreamForRead<TInstance>(IAbstractDataStream<TInstance> dataStream)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            AddAccessWrapper(new DataStreamActiveAccessWrapper<TInstance>((EntityProxyDataStream<TInstance>)dataStream, AccessType.SharedRead, Usage.Default));
            return this;
        }

        public IJobConfig RequestCancelFor(AbstractTaskDriver taskDriver)
        {
            AddAccessWrapper(new CancelRequestsPendingAccessWrapper(taskDriver.TaskSet.CancelRequestsDataStream, AccessType.SharedWrite, Usage.RequestCancel));
            return this;
        }
        
        // public IJobConfig RequireTaskDriverForRequestCancel(AbstractTaskDriver taskDriver)
        // {
        //     AddAccessWrapper(new CancelFlowAccessWrapper(taskDriver.TaskSet.CancelFlow, AccessType.SharedWrite, Usage.Write));
        //     return this;
        // }
        //

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - GENERIC DATA
        //*************************************************************************************************************
        
        public IJobConfig RequireGenericDataForRead<TData>(AccessControlledValue<TData> collection)
            where TData : struct
        {
            AddAccessWrapper(new GenericDataAccessWrapper<TData>(collection, AccessType.SharedRead, Usage.Default));
            return this;
        }
        
        public IJobConfig RequireGenericDataForWrite<TData>(AccessControlledValue<TData> collection)
            where TData : struct
        {
            AddAccessWrapper(new GenericDataAccessWrapper<TData>(collection, AccessType.SharedWrite, Usage.Default));
            return this;
        }
        
        public IJobConfig RequireGenericDataForExclusiveWrite<TData>(AccessControlledValue<TData> collection)
            where TData : struct
        {
            AddAccessWrapper(new GenericDataAccessWrapper<TData>(collection, AccessType.ExclusiveWrite, Usage.Default));
            return this;
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - ENTITY QUERY
        //*************************************************************************************************************
        
        public IJobConfig RequireEntityNativeArrayFromQueryForRead(EntityQuery entityQuery)
        {
            return RequireEntityNativeArrayFromQueryForRead(new EntityQueryNativeArray(entityQuery));
        }
        
        public IJobConfig RequireIComponentDataNativeArrayFromQueryForRead<T>(EntityQuery entityQuery)
            where T : struct, IComponentData
        {
            return RequireIComponentDataNativeArrayFromQueryForRead(new EntityQueryComponentNativeArray<T>(entityQuery));
        }

        protected IJobConfig RequireEntityNativeArrayFromQueryForRead(EntityQueryNativeArray entityQueryNativeArray)
        {
            AddAccessWrapper(new EntityQueryAccessWrapper(entityQueryNativeArray, Usage.Default));
            return this;
        }

        protected IJobConfig RequireIComponentDataNativeArrayFromQueryForRead<T>(EntityQueryComponentNativeArray<T> entityQueryNativeArray)
            where T : struct, IComponentData
        {
            AddAccessWrapper(new EntityQueryComponentAccessWrapper<T>(entityQueryNativeArray, Usage.Default));
            return this;
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - ComponentDataFromEntity (CDFE)
        //*************************************************************************************************************

        //TODO: #86 - Revisit this section after Entities 1.0 upgrade for name changes to CDFE
        public IJobConfig RequireCDFEForRead<T>()
            where T : struct, IComponentData
        {
            AddAccessWrapper(new CDFEAccessWrapper<T>(AccessType.SharedRead, Usage.Default, TaskSetOwner.TaskDriverSystem));
            return this;
        }
        
        public IJobConfig RequireCDFEForWrite<T>()
            where T : struct, IComponentData
        {
            AddAccessWrapper(new CDFEAccessWrapper<T>(AccessType.SharedWrite, Usage.Default, TaskSetOwner.TaskDriverSystem));
            return this;
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DynamicBuffer
        //*************************************************************************************************************
        
        public IJobConfig RequireDBFEForRead<T>()
            where T : struct, IBufferElementData
        {
            AddAccessWrapper(new DynamicBufferAccessWrapper<T>(AccessType.SharedRead, Usage.Default, TaskSetOwner.TaskDriverSystem));

            return this;
        }
        
        public IJobConfig RequireDBFEForExclusiveWrite<T>()
            where T : struct, IBufferElementData
        {
            AddAccessWrapper(new DynamicBufferAccessWrapper<T>(AccessType.ExclusiveWrite, Usage.Default, TaskSetOwner.TaskDriverSystem));
            return this;
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

            foreach (AbstractAccessWrapper wrapper in m_AccessWrappers.Values)
            {
                m_SchedulingAccessWrappers.Add(wrapper);
            }

            m_AccessWrapperDependencies = new NativeArray<JobHandle>(m_SchedulingAccessWrappers.Count + 1, Allocator.Persistent);
        }

        protected virtual void HardenConfig()
        {
        }

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

            int index = 0;
            for (; index < m_SchedulingAccessWrappers.Count; ++index)
            {
                m_AccessWrapperDependencies[index] = m_SchedulingAccessWrappers[index].Acquire();
            }

            m_AccessWrapperDependencies[index] = dependsOn;

            dependsOn = JobHandle.CombineDependencies(m_AccessWrapperDependencies);
            dependsOn = m_ScheduleInfo.CallScheduleFunction(dependsOn);

            foreach (AbstractAccessWrapper wrapper in m_SchedulingAccessWrappers)
            {
                wrapper.Release(dependsOn);
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

        internal CancelCompleteDataStream GetCancelCompleteDataStream()
        {
            CancelCompleteActiveAccessWrapper cancelCompleteActiveAccessWrapper = GetAccessWrapper<CancelCompleteActiveAccessWrapper>(Usage.CancelComplete);
            return cancelCompleteActiveAccessWrapper.CancelCompleteDataStream;
        }

        // internal UnsafeParallelHashMap<EntityProxyInstanceID, bool> GetCancelProgressLookup(Usage usage)
        // {
        //     CancelProgressLookupAccessWrapper cancelProgressLookupAccessWrapper = GetAccessWrapper<CancelProgressLookupAccessWrapper>(usage);
        //     return cancelProgressLookupAccessWrapper.ProgressLookup;
        // }

        // internal PendingCancelDataStream<TInstance> GetPendingCancelDataStream<TInstance>(Usage usage)
        //     where TInstance : unmanaged, IEntityProxyInstance
        // {
        //     PendingCancelDataStreamAccessWrapper<TInstance> pendingCancelDataStreamAccessWrapper = GetAccessWrapper<PendingCancelDataStreamAccessWrapper<TInstance>>(usage);
        //     return pendingCancelDataStreamAccessWrapper.PendingCancelDataStream;
        // }

        internal CancelRequestsDataStream GetCancelRequestsDataStream()
        {
            CancelRequestsPendingAccessWrapper cancelRequestsPendingAccessWrapper = GetAccessWrapper<CancelRequestsPendingAccessWrapper>(Usage.RequestCancel);
            return cancelRequestsPendingAccessWrapper.CancelRequestsDataStream;
        }

        internal EntityProxyDataStream<TInstance> GetPendingDataStream<TInstance>(Usage usage)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            DataStreamPendingAccessWrapper<TInstance> dataStreamAccessWrapper = GetAccessWrapper<DataStreamPendingAccessWrapper<TInstance>>(usage);
            return dataStreamAccessWrapper.DataStream;
        }
        
        internal EntityProxyDataStream<TInstance> GetActiveDataStream<TInstance>(Usage usage)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            DataStreamActiveAccessWrapper<TInstance> dataStreamAccessWrapper = GetAccessWrapper<DataStreamActiveAccessWrapper<TInstance>>(usage);
            return dataStreamAccessWrapper.DataStream;
        }

        internal EntityProxyDataStream<TInstance> GetPendingCancelDataStream<TInstance>()
            where TInstance : unmanaged, IEntityProxyInstance
        {
            DataStreamPendingCancelActiveAccessWrapper<TInstance> dataStreamPendingCancelActiveAccessWrapper = GetAccessWrapper<DataStreamPendingCancelActiveAccessWrapper<TInstance>>(Usage.Cancelling);
            return dataStreamPendingCancelActiveAccessWrapper.DataStream;
        }

        // internal TaskDriverCancelFlow GetCancelFlow(Usage usage)
        // {
        //     CancelFlowAccessWrapper cancelFlowAccessWrapper = GetAccessWrapper<CancelFlowAccessWrapper>(usage);
        //     return cancelFlowAccessWrapper.CancelFlow;
        // }

        internal TData GetGenericData<TData>()
            where TData : struct
        {
            GenericDataAccessWrapper<TData> genericDataAccessWrapper = GetAccessWrapper<GenericDataAccessWrapper<TData>>(Usage.Default);
            return genericDataAccessWrapper.Data;
        }

        internal NativeArray<Entity> GetEntityNativeArrayFromQuery()
        {
            EntityQueryAccessWrapper entityQueryAccessWrapper = GetAccessWrapper<EntityQueryAccessWrapper>(Usage.Default);
            return entityQueryAccessWrapper.NativeArray;
        }

        internal NativeArray<T> GetIComponentDataNativeArrayFromQuery<T>()
            where T : struct, IComponentData
        {
            EntityQueryComponentAccessWrapper<T> entityQueryAccessWrapper = GetAccessWrapper<EntityQueryComponentAccessWrapper<T>>(Usage.Default);
            return entityQueryAccessWrapper.NativeArray;
        }

        internal CDFEReader<T> GetCDFEReader<T>()
            where T : struct, IComponentData
        {
            CDFEAccessWrapper<T> cdfeAccessWrapper = GetAccessWrapper<CDFEAccessWrapper<T>>(Usage.Default);
            return cdfeAccessWrapper.CreateCDFEReader();
        }

        internal CDFEWriter<T> GetCDFEWriter<T>()
            where T : struct, IComponentData
        {
            CDFEAccessWrapper<T> cdfeAccessWrapper = GetAccessWrapper<CDFEAccessWrapper<T>>(Usage.Default);
            return cdfeAccessWrapper.CreateCDFEUpdater();
        }

        internal DBFEForRead<T> GetDBFEForRead<T>()
            where T : struct, IBufferElementData
        {
            DynamicBufferAccessWrapper<T> dynamicBufferAccessWrapper = GetAccessWrapper<DynamicBufferAccessWrapper<T>>(Usage.Default);
            return dynamicBufferAccessWrapper.CreateDynamicBufferReader();
        }

        internal DBFEForExclusiveWrite<T> GetDBFEForExclusiveWrite<T>()
            where T : struct, IBufferElementData
        {
            DynamicBufferAccessWrapper<T> dynamicBufferAccessWrapper = GetAccessWrapper<DynamicBufferAccessWrapper<T>>(Usage.Default);
            return dynamicBufferAccessWrapper.CreateDynamicBufferExclusiveWriter();
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureIsHardened()
        {
            if (m_IsHardened == false)
            {
                throw new InvalidOperationException($"{this} is not hardened yet!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNotHardened()
        {
            if (m_IsHardened == true)
            {
                throw new InvalidOperationException($"{this} is already hardened!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureWrapperExists(JobConfigDataID id)
        {
            if (!m_AccessWrappers.ContainsKey(id))
            {
                throw new InvalidOperationException($"Job configured by {this} tried to access {id.AccessWrapperType.GetReadableName()} data for {id.Usage} but it wasn't found. Did you call the right Require function?");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureWrapperValidity(JobConfigDataID id)
        {
            //Straight duplicate check
            if (m_AccessWrappers.ContainsKey(id))
            {
                throw new InvalidOperationException($"{this} is trying to require {id.AccessWrapperType.GetReadableName()} data for {id.Usage} but it is already being used! Only require the data for the same usage once!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureWrapperUsage(AbstractAccessWrapper wrapper)
        {
            if (wrapper.Debug_WrapperType != typeof(AbstractDataStreamAccessWrapper<>))
            {
                return;
            }
            
            //TODO: Fix?

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

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureWrapperUsageValid(JobConfigDataID id, params Usage[] allowedUsages)
        {
            foreach (Usage usage in USAGE_TYPES)
            {
                //Don't check against ourself or any of the allowed usages
                if (id.Usage == usage
                 || allowedUsages.Contains(usage))
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

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNoScheduleInfo()
        {
            if (m_ScheduleInfo != null)
            {
                throw new InvalidOperationException($"{this} is trying to schedule a job but it already has Schedule Info {m_ScheduleInfo} defined! Only schedule one piece of data!");
            }
        }


        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureScheduleInfoExists()
        {
            if (m_ScheduleInfo == null)
            {
                throw new InvalidOperationException($"{this} does not have a {nameof(AbstractScheduleInfo)} yet! Please schedule on some data first.");
            }
        }
    }
}
