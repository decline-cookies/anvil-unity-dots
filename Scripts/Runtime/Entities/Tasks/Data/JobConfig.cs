using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public class JobConfig
    {
        internal enum Usage
        {
            //TODO: Docs
            Update,
            Write,
            Read
        }

        internal static readonly BulkScheduleDelegate<JobConfig> PREPARE_AND_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<JobConfig>(nameof(PrepareAndSchedule), BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly Usage[] USAGE_TYPES = (Usage[])Enum.GetValues(typeof(Usage));

        //TODO: Docs
        public delegate JobHandle ScheduleJobDelegate(JobHandle jobHandle, JobData jobData, IScheduleInfo scheduleInfo);


        private readonly ScheduleJobDelegate m_ScheduleJobFunction;
        private readonly Dictionary<JobConfigDataID, DataStreamAccessWrapper> m_DataStreamAccessWrappers;
        private readonly Dictionary<JobConfigDataID, NativeArrayAccessWrapper> m_NativeArrayAccessWrappers;
        private readonly Dictionary<JobConfigDataID, EntityQueryAccessWrapper> m_EntityQueryAccessWrappers;
        private readonly ITaskFlowGraph m_TaskFlowGraph;
        private readonly ITaskSystem m_TaskSystem;
        private readonly ITaskDriver m_TaskDriver;
        private readonly JobData m_JobData;

        private IScheduleInfo m_ScheduleInfo;

        internal JobConfig(ITaskFlowGraph taskFlowGraph, ITaskSystem taskSystem, ITaskDriver taskDriver, ScheduleJobDelegate scheduleJobFunction)
        {
            m_TaskFlowGraph = taskFlowGraph;
            m_TaskSystem = taskSystem;
            m_TaskDriver = taskDriver;
            m_ScheduleJobFunction = scheduleJobFunction;
            
            m_DataStreamAccessWrappers = new Dictionary<JobConfigDataID, DataStreamAccessWrapper>();
            m_NativeArrayAccessWrappers = new Dictionary<JobConfigDataID, NativeArrayAccessWrapper>();
            m_EntityQueryAccessWrappers = new Dictionary<JobConfigDataID, EntityQueryAccessWrapper>();
            
            m_JobData = new JobData(m_TaskSystem.World,
                                    m_TaskDriver?.Context ?? m_TaskSystem.Context,
                                    this);
        }

        public override string ToString()
        {
            return $"{nameof(JobConfig)} with schedule function name of {m_ScheduleJobFunction.Method.DeclaringType?.Name}.{m_ScheduleJobFunction.Method.Name} on {TaskDebugUtil.GetLocation(m_TaskSystem, m_TaskDriver)}";
        }

        //*************************************************************************************************************
        // CONFIGURATION - SCHEDULING
        //*************************************************************************************************************

        public JobConfig ScheduleOn<TInstance>(ProxyDataStream<TInstance> dataStream, BatchStrategy batchStrategy)
            where TInstance : unmanaged, IProxyInstance
        {
            //TODO: Ensure nothing else was called.
            Debug_EnsureNoScheduleInfo();
            m_ScheduleInfo = new ProxyDataStreamScheduleInfo<TInstance>(dataStream, batchStrategy);
            return this;
        }

        public JobConfig ScheduleOn<T>(NativeArray<T> nativeArray, BatchStrategy batchStrategy)
            where T : unmanaged
        {
            //TODO: Ensure nothing else was called.
            Debug_EnsureNoScheduleInfo();
            m_ScheduleInfo = new NativeArrayScheduleInfo<T>(nativeArray, batchStrategy);
            return this;
        }

        public JobConfig ScheduleOn(EntityQuery entityQuery, BatchStrategy batchStrategy)
        {
            //TODO: Ensure nothing else was called.
            Debug_EnsureNoScheduleInfo();
            m_ScheduleInfo = new EntityQueryScheduleInfo(entityQuery, batchStrategy);
            return this;
        }

        //TODO: Add in ScheduleOn for Query Components

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DATA STREAM
        //*************************************************************************************************************

        public JobConfig RequireDataStreamForUpdate(AbstractProxyDataStream dataStream)
        {
            //TODO: Ensure ScheduleInfo exists
            JobConfigDataID id = new JobConfigDataID(dataStream, Usage.Update);
            Debug_EnsureWrapperValidity(id);
            m_DataStreamAccessWrappers.Add(id,
                                           new DataStreamAccessWrapper(dataStream, AccessType.ExclusiveWrite));
            return this;
        }

        public JobConfig RequireDataStreamForWrite(AbstractProxyDataStream dataStream)
        {
            //TODO: Ensure ScheduleInfo exists
            JobConfigDataID id = new JobConfigDataID(dataStream, Usage.Write);
            Debug_EnsureWrapperValidity(id);
            m_DataStreamAccessWrappers.Add(id,
                                           new DataStreamAccessWrapper(dataStream, AccessType.SharedWrite));
            return this;
        }

        public JobConfig RequireDataStreamForRead(AbstractProxyDataStream dataStream)
        {
            //TODO: Ensure ScheduleInfo exists
            JobConfigDataID id = new JobConfigDataID(dataStream, Usage.Read);
            Debug_EnsureWrapperValidity(id);
            m_DataStreamAccessWrappers.Add(id,
                                           new DataStreamAccessWrapper(dataStream, AccessType.SharedRead));
            return this;
        }

        public JobConfig RequireResolveChannel<TResolveChannel>(TResolveChannel resolveChannel)
            where TResolveChannel : Enum
        {
            //TODO: Ensure ScheduleInfo exists
            ResolveChannelUtil.Debug_EnsureEnumValidity(resolveChannel);

            //Any data streams that have registered for this resolve channel type either on the system or related task drivers will be needed.
            //When the updater runs, it doesn't know yet which resolve channel a particular instance will resolve to yet until it actually resolves.
            //We need to ensure that all possible locations have write access
            List<AbstractProxyDataStream> dataStreams = m_TaskFlowGraph.GetResolveChannelDataStreams(resolveChannel, m_TaskSystem);
            foreach (AbstractProxyDataStream dataStream in dataStreams)
            {
                RequireDataStreamForWrite(dataStream);
            }

            //TODO: Build the DOTS Hashmap of pointers to write to.

            return this;
        }
        
        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - NATIVE ARRAY
        //*************************************************************************************************************
        public JobConfig RequireNativeArrayForWrite<T>(NativeArray<T> array)
            where T : unmanaged
        {
            //TODO: Ensure ScheduleInfo exists
            JobConfigDataID id = new JobConfigDataID(typeof(NativeArray<T>), Usage.Write);
            Debug_EnsureWrapperValidity(id);
            m_NativeArrayAccessWrappers.Add(id,
                                            NativeArrayAccessWrapper.Create(array));
            return this;
        }
        
        public JobConfig RequireNativeArrayForRead<T>(NativeArray<T> array)
            where T : unmanaged
        {
            //TODO: Ensure ScheduleInfo exists
            JobConfigDataID id = new JobConfigDataID(typeof(NativeArray<T>), Usage.Read);
            Debug_EnsureWrapperValidity(id);
            m_NativeArrayAccessWrappers.Add(id,
                                            NativeArrayAccessWrapper.Create(array));
            return this;
        }
        
        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - ENTITY QUERY
        //*************************************************************************************************************
        
        public JobConfig RequireEntityArrayFromQueryForRead(EntityQuery entityQuery)
        {
            //TODO: Ensure ScheduleInfo exists
            JobConfigDataID id = new JobConfigDataID(typeof(EntityQueryAccessWrapper.EntityQueryType<Entity>), Usage.Read);
            Debug_EnsureWrapperValidity(id);
            m_EntityQueryAccessWrappers.Add(id,
                                            new EntityQueryAccessWrapper(entityQuery));
            
            //TODO: Check schedule info and marry if query matches
            return this;
        }

        //*************************************************************************************************************
        // EXECUTION
        //*************************************************************************************************************

        //TODO: Cross reference with JobTaskWorkConfig to include safety checks and other data
        private JobHandle PrepareAndSchedule(JobHandle dependsOn)
        {
            //TODO Harden this so we can get optimized structures
            foreach (DataStreamAccessWrapper wrapper in m_DataStreamAccessWrappers.Values)
            {
                //TODO: Convert to native array
                dependsOn = JobHandle.CombineDependencies(dependsOn, wrapper.DataStream.AccessController.AcquireAsync(wrapper.AccessType));
            }

            foreach (EntityQueryAccessWrapper wrapper in m_EntityQueryAccessWrappers.Values)
            {
                dependsOn = JobHandle.CombineDependencies(dependsOn, wrapper.Acquire());
            }

            //TODO: Async any entity queries we might be waiting on and ensure the job handles are part of dependsOn

            dependsOn = m_ScheduleJobFunction(dependsOn, m_JobData, m_ScheduleInfo);

            foreach (DataStreamAccessWrapper wrapper in m_DataStreamAccessWrappers.Values)
            {
                wrapper.DataStream.AccessController.ReleaseAsync(dependsOn);
            }

            foreach (EntityQueryAccessWrapper wrapper in m_EntityQueryAccessWrappers.Values)
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
            return (ProxyDataStream<TInstance>)m_DataStreamAccessWrappers[id].DataStream;
        }

        internal NativeArray<T> GetNativeArray<T>(Usage usage)
            where T : unmanaged
        {
            JobConfigDataID id = new JobConfigDataID(typeof(NativeArray<T>), usage);
            Debug_EnsureWrapperExists(id);
            return m_NativeArrayAccessWrappers[id].ResolveNativeArray<T>();
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNoScheduleInfo()
        {
            if (m_ScheduleInfo != null)
            {
                throw new InvalidOperationException($"{this} is trying to schedule a job but it already has Schedule Info {m_ScheduleInfo} defined! Only call {nameof(ScheduleOn)} once!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureWrapperExists(JobConfigDataID id)
        {
            if (!m_DataStreamAccessWrappers.ContainsKey(id))
            {
                throw new InvalidOperationException($"Job configured by {this} tried to access {id.Type} data for {id.Usage} but it wasn't found. Did you call the right Require function?");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureWrapperValidity(JobConfigDataID id)
        {
            //Straight duplicate check
            if (m_DataStreamAccessWrappers.ContainsKey(id))
            {
                throw new InvalidOperationException($"{this} is trying to require {id.Type} data for {id.Usage} but it is already being used! Only require the data for the same usage once!");
            }
            
            //TODO: Can't go here if NativeArray or EntityQuery
            //Access checks
            switch (id.Usage)
            {
                case Usage.Update:
                    Debug_EnsureWrapperTypesValid(id);
                    break;
                case Usage.Write:
                    Debug_EnsureWrapperTypesValid(id, Usage.Read);
                    break;
                case Usage.Read:
                    Debug_EnsureWrapperTypesValid(id, Usage.Write);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Trying to switch on {nameof(id.Usage)} but no code path satisfies for {id.Usage}!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureWrapperTypesValid(JobConfigDataID id, params Usage[] allowedUsages)
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
                if (m_DataStreamAccessWrappers.ContainsKey(checkID))
                {
                    throw new InvalidOperationException($"{this} is trying to require {id.Type} data for {id.Usage} but the same type is being used for {usage} which is not allowed!");
                }
            }
        }
    }
}
