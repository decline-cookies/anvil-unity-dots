using Anvil.CSharp.Core;
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
                                                IJobConfigRequirements
    {
        internal enum Usage
        {
            //TODO: Docs
            Update,
            Write,
            Read,
            WritePendingCancel,
            Cancelling,
            Resolve
        }

        internal static readonly BulkScheduleDelegate<AbstractJobConfig> PREPARE_AND_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<AbstractJobConfig>(nameof(PrepareAndSchedule), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly Usage[] USAGE_TYPES = (Usage[])Enum.GetValues(typeof(Usage));

        private readonly string m_TypeString;
        private readonly Dictionary<JobConfigDataID, IAccessWrapper> m_AccessWrappers;
        private readonly List<IAccessWrapper> m_SchedulingAccessWrappers;
        
        private NativeArray<JobHandle> m_AccessWrapperDependencies;
        private AbstractScheduleInfo m_ScheduleInfo;
        private bool m_ShouldDisableAfterNextRun;
        private bool m_IsHardened;

        public bool IsEnabled
        {
            get;
            set;
        }
        
        protected TaskFlowGraph TaskFlowGraph { get; }
        protected internal AbstractTaskSystem TaskSystem { get; }
        protected internal AbstractTaskDriver TaskDriver { get; }
        

        protected AbstractJobConfig(TaskFlowGraph taskFlowGraph, 
                                    AbstractTaskSystem taskSystem, 
                                    AbstractTaskDriver taskDriver)
        {
            IsEnabled = true;
            Type type = GetType();

            //TODO: #112 (c-sharp-core) Extract to Anvil-CSharp Util method -Used in AbstractProxyDataStream as well
            m_TypeString = type.IsGenericType
                ? $"{type.Name[..^2]}<{type.GenericTypeArguments[0].Name}>"
                : type.Name;
            
            TaskFlowGraph = taskFlowGraph;
            TaskSystem = taskSystem;
            TaskDriver = taskDriver;

            m_AccessWrappers = new Dictionary<JobConfigDataID, IAccessWrapper>();
            m_SchedulingAccessWrappers = new List<IAccessWrapper>();
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

            foreach (IAccessWrapper wrapper in m_AccessWrappers.Values)
            {
                wrapper.Dispose();
            }

            base.DisposeSelf();
        }

        public override string ToString()
        {
            return $"{m_TypeString} with schedule function name of {m_ScheduleInfo?.ScheduleJobFunctionDebugInfo ?? "NOT YET SET"} on {TaskDebugUtil.GetLocationName(TaskSystem, TaskDriver)}";
            
        }

        //*************************************************************************************************************
        // CONFIGURATION - COMMON
        //*************************************************************************************************************

        public IJobConfig RunOnce()
        {
            m_ShouldDisableAfterNextRun = true;
            return this;
        }

        protected void AddAccessWrapper(JobConfigDataID id, IAccessWrapper accessWrapper)
        {
            Debug_EnsureWrapperValidity(id);
            Debug_EnsureWrapperUsage(id, accessWrapper);
            m_AccessWrappers.Add(id, accessWrapper);
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DATA STREAM
        //*************************************************************************************************************

        public IJobConfigRequirements RequireTaskStreamForWrite<TInstance>(TaskStream<TInstance> taskStream)
            where TInstance : unmanaged, IProxyInstance
        {
            return RequireDataStreamForWrite(taskStream.DataStream, Usage.Write);
        }

        protected IJobConfigRequirements RequireCancellableTaskStreamForWrite<TInstance>(CancellableTaskStream<TInstance> cancellableTaskStream)
            where TInstance : unmanaged, IProxyInstance
        {
            return RequireDataStreamForWrite(cancellableTaskStream.PendingCancelDataStream, Usage.WritePendingCancel);
        }

        private IJobConfigRequirements RequireDataStreamForWrite(AbstractProxyDataStream dataStream, Usage usage)
        {
            AddAccessWrapper(new JobConfigDataID(dataStream, usage),
                             new DataStreamAccessWrapper(dataStream, AccessType.SharedWrite));
            return this;
        }

        public IJobConfigRequirements RequireTaskStreamForRead<TInstance>(TaskStream<TInstance> taskStream)
            where TInstance : unmanaged, IProxyInstance
        {
            AddAccessWrapper(new JobConfigDataID(taskStream.DataStream, Usage.Read),
                             new DataStreamAccessWrapper(taskStream.DataStream, AccessType.SharedRead));
            
            return this;
        }

        public IJobConfigRequirements RequireTaskDriverForRequestCancel(AbstractTaskDriver taskDriver)
        {
            CancelRequestsDataStream cancelRequestsDataStream = taskDriver.CancelRequestsDataStream;
            AddAccessWrapper(new JobConfigDataID(cancelRequestsDataStream, Usage.Write),
                             new CancelRequestsAccessWrapper(cancelRequestsDataStream, AccessType.SharedWrite, taskDriver.Context));
            
            return this;
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - NATIVE ARRAY
        //*************************************************************************************************************
        public IJobConfigRequirements RequireNativeArrayForWrite<T>(NativeArray<T> array)
            where T : struct
        {
            AddAccessWrapper(new JobConfigDataID(typeof(NativeArray<T>), Usage.Write),
                             NativeArrayAccessWrapper.Create(array));
            
            return this;
        }

        public IJobConfigRequirements RequireNativeArrayForRead<T>(NativeArray<T> array)
            where T : struct
        {
            AddAccessWrapper(new JobConfigDataID(typeof(NativeArray<T>), Usage.Read),
                             NativeArrayAccessWrapper.Create(array));
            return this;
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - ENTITY QUERY
        //*************************************************************************************************************

        public IJobConfigRequirements RequireEntityNativeArrayFromQueryForRead(EntityQuery entityQuery)
        {
            return RequireEntityNativeArrayFromQueryForRead(new EntityQueryNativeArray(entityQuery));
        }

        public IJobConfigRequirements RequireIComponentDataNativeArrayFromQueryForRead<T>(EntityQuery entityQuery)
            where T : struct, IComponentData
        {
            return RequireIComponentDataNativeArrayFromQueryForRead(new EntityQueryComponentNativeArray<T>(entityQuery));
        }
        
        protected IJobConfigRequirements RequireEntityNativeArrayFromQueryForRead(EntityQueryNativeArray entityQueryNativeArray)
        {
            EntityQueryAccessWrapper wrapper = new EntityQueryAccessWrapper(entityQueryNativeArray);
            AddAccessWrapper(new JobConfigDataID(typeof(EntityQueryAccessWrapper.EntityQueryType<Entity>), Usage.Read),
                             wrapper);

            return this;
        }
        
        protected IJobConfigRequirements RequireIComponentDataNativeArrayFromQueryForRead<T>(EntityQueryComponentNativeArray<T> entityQueryNativeArray)
            where T : struct, IComponentData
        {
            EntityQueryComponentAccessWrapper<T> wrapper = new EntityQueryComponentAccessWrapper<T>(entityQueryNativeArray);
            AddAccessWrapper(new JobConfigDataID(typeof(EntityQueryComponentAccessWrapper<T>.EntityQueryComponentType), Usage.Read),
                             wrapper);

            return this;
        }

        //*************************************************************************************************************
        // HARDEN
        //*************************************************************************************************************

        public void Harden()
        {
            if (m_IsHardened)
            {
                return;
            }

            m_IsHardened = true;

            foreach (IAccessWrapper wrapper in m_AccessWrappers.Values)
            {
                m_SchedulingAccessWrappers.Add(wrapper);
            }

            m_AccessWrapperDependencies = new NativeArray<JobHandle>(m_SchedulingAccessWrappers.Count + 1, Allocator.Persistent);
            
            HardenConfig();
        }

        protected virtual void HardenConfig()
        {
            
        }

        //*************************************************************************************************************
        // EXECUTION
        //*************************************************************************************************************

        private JobHandle PrepareAndSchedule(JobHandle dependsOn)
        {
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

            foreach (IAccessWrapper wrapper in m_SchedulingAccessWrappers)
            {
                wrapper.Release(dependsOn);
            }

            return dependsOn;
        }

        internal ProxyDataStream<TInstance> GetDataStream<TInstance>(Usage usage)
            where TInstance : unmanaged, IProxyInstance
        {
            JobConfigDataID id = new JobConfigDataID(typeof(ProxyDataStream<TInstance>), usage);
            Debug_EnsureWrapperExists(id);
            DataStreamAccessWrapper dataStreamAccessWrapper = (DataStreamAccessWrapper)m_AccessWrappers[id];
            return (ProxyDataStream<TInstance>)dataStreamAccessWrapper.DataStream;
        }

        internal CancelRequestsDataStream GetCancelRequestsDataStream(Usage usage)
        {
            JobConfigDataID id = new JobConfigDataID(typeof(CancelRequestsDataStream), usage);
            Debug_EnsureWrapperExists(id);
            CancelRequestsAccessWrapper dataStreamAccessWrapper = (CancelRequestsAccessWrapper)m_AccessWrappers[id];
            return dataStreamAccessWrapper.CancelRequestsDataStream;
        }
        
        internal void GetCancelRequestsDataStreamWithContext(Usage usage, out CancelRequestsDataStream dataStream, out byte context)
        {
            JobConfigDataID id = new JobConfigDataID(typeof(CancelRequestsDataStream), usage);
            Debug_EnsureWrapperExists(id);
            CancelRequestsAccessWrapper dataStreamAccessWrapper = (CancelRequestsAccessWrapper)m_AccessWrappers[id];
            dataStream = dataStreamAccessWrapper.CancelRequestsDataStream;
            context = dataStreamAccessWrapper.Context;
        }
        

        internal NativeArray<T> GetNativeArray<T>(Usage usage)
            where T : unmanaged
        {
            JobConfigDataID id = new JobConfigDataID(typeof(NativeArray<T>), usage);
            Debug_EnsureWrapperExists(id);
            NativeArrayAccessWrapper nativeArrayAccessWrapper = (NativeArrayAccessWrapper)m_AccessWrappers[id];
            return nativeArrayAccessWrapper.ResolveNativeArray<T>();
        }

        internal NativeArray<Entity> GetEntityNativeArrayFromQuery(Usage usage)
        {
            JobConfigDataID id = new JobConfigDataID(typeof(EntityQueryAccessWrapper.EntityQueryType<Entity>), usage);
            Debug_EnsureWrapperExists(id);
            EntityQueryAccessWrapper entityQueryAccessWrapper = (EntityQueryAccessWrapper)m_AccessWrappers[id];
            return entityQueryAccessWrapper.NativeArray;
        }

        internal NativeArray<T> GetIComponentNativeArrayFromQuery<T>(Usage usage)
            where T : struct, IComponentData
        {
            JobConfigDataID id = new JobConfigDataID(typeof(EntityQueryComponentAccessWrapper<T>.EntityQueryComponentType), usage);
            Debug_EnsureWrapperExists(id);
            EntityQueryComponentAccessWrapper<T> entityQueryAccessWrapper = (EntityQueryComponentAccessWrapper<T>)m_AccessWrappers[id];
            return entityQueryAccessWrapper.NativeArray;
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
        private void Debug_EnsureWrapperExists(JobConfigDataID id)
        {
            if (!m_AccessWrappers.ContainsKey(id))
            {
                throw new InvalidOperationException($"Job configured by {this} tried to access {id.Type} data for {id.Usage} but it wasn't found. Did you call the right Require function?");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureWrapperValidity(JobConfigDataID id)
        {
            //Straight duplicate check
            if (m_AccessWrappers.ContainsKey(id))
            {
                throw new InvalidOperationException($"{this} is trying to require {id.Type} data for {id.Usage} but it is already being used! Only require the data for the same usage once!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureWrapperUsage(JobConfigDataID id, IAccessWrapper wrapper)
        {
            if (wrapper is not DataStreamAccessWrapper)
            {
                return;
            }
            
            //Access checks
            switch (id.Usage)
            {
                case Usage.Update:
                    //While updating, the same type could be cancelling.
                    Debug_EnsureWrapperUsageValid(id, Usage.WritePendingCancel);
                    break;
                case Usage.Write:
                    //Allowed to read while writing because we are writing to UnsafeTypedStream and reading from NativeArray
                    Debug_EnsureWrapperUsageValid(id, Usage.Read);
                    break;
                case Usage.Read:
                    //Allowed to write while reading because we are writing to UnsafeTypedStream and reading from NativeArray
                    Debug_EnsureWrapperUsageValid(id, Usage.Write);
                    break;
                case Usage.WritePendingCancel:
                    //We'll be updating when writing to cancel
                    Debug_EnsureWrapperUsageValid(id, Usage.Update);
                    break;
                case Usage.Cancelling:
                    //When we're cancelling, we can read or write to others because we're operating on a different stream
                    Debug_EnsureWrapperUsageValid(id, Usage.Read, Usage.Write);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Trying to switch on {nameof(id.Usage)} but no code path satisfies for {id.Usage}!");
            }
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

                JobConfigDataID checkID = new JobConfigDataID(id.Type, usage);
                if (m_AccessWrappers.ContainsKey(checkID))
                {
                    throw new InvalidOperationException($"{this} is trying to require {id.Type} data for {id.Usage} but the same type is being used for {usage} which is not allowed!");
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
                throw new InvalidOperationException($"{this} does not have a {nameof(IScheduleInfo)} yet! Please schedule on some data first.");
            }
        }



        //TODO: See where this can fit during hardening
        // [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        // private void Debug_EnsureDataStreamIntegrity(AbstractProxyDataStream dataStream, Type expectedType, AbstractTaskDriver taskDriver)
        // {
        //     if (dataStream == null)
        //     {
        //         throw new InvalidOperationException($"Data Stream is null! Possible causes: "
        //                                           + $"\n1. The incorrect reference to a {expectedType.Name} was passed in such as referencing a hidden variable or something not defined on this class or one of this classes TaskDrivers. {typeof(ProxyDataStream<>)}'s are created via reflection in the constructor of this class and TaskDrivers."
        //                                           + $"\n2. The {nameof(ConfigureJobFor)} function wasn't called from {nameof(OnCreate)}. The reflection to create {expectedType.Name}'s hasn't happened yet.");
        //     }
        //
        //     if (!m_TaskFlowGraph.IsDataStreamRegistered(dataStream, this, taskDriver))
        //     {
        //         throw new InvalidOperationException($"DataStream of {dataStream} was not registered with the {nameof(TaskFlowGraph)}! Was it defined as a part of this class or TaskDrivers associated with this class?");
        //     }
        // }
    }
}
