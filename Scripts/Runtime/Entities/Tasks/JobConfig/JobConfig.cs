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
    internal class JobConfig : IScheduleJobConfig,
                               IScheduleUpdateJobConfig,
                               IUpdateJobConfig,
                               IJobConfig
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
        

        private readonly IJobConfig.ScheduleJobDelegate m_ScheduleJobFunction;
        private readonly Dictionary<JobConfigDataID, IAccessWrapper> m_AccessWrappers;
        private readonly TaskFlowGraph m_TaskFlowGraph;
        private readonly ITaskSystem m_TaskSystem;
        private readonly ITaskDriver m_TaskDriver;
        private readonly JobData m_JobData;

        private IScheduleInfo m_ScheduleInfo;

        public JobConfig(TaskFlowGraph taskFlowGraph, ITaskSystem taskSystem, ITaskDriver taskDriver, IJobConfig.ScheduleJobDelegate scheduleJobFunction)
        {
            m_TaskFlowGraph = taskFlowGraph;
            m_TaskSystem = taskSystem;
            m_TaskDriver = taskDriver;
            m_ScheduleJobFunction = scheduleJobFunction;
            
            m_AccessWrappers = new Dictionary<JobConfigDataID, IAccessWrapper>();

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

        IUpdateJobConfig IScheduleUpdateJobConfig.ScheduleOn<TInstance>(ProxyDataStream<TInstance> dataStream, BatchStrategy batchStrategy)
        {
            return (IUpdateJobConfig)ScheduleOn(dataStream, batchStrategy);
        }

        public IJobConfig ScheduleOn<TInstance>(ProxyDataStream<TInstance> dataStream, BatchStrategy batchStrategy)
            where TInstance : unmanaged, IProxyInstance
        {
            Debug_EnsureNoData();
            Debug_EnsureNoScheduleInfo();
            m_ScheduleInfo = new ProxyDataStreamScheduleInfo<TInstance>(dataStream, batchStrategy);
            return this;
        }

        public IJobConfig ScheduleOn<T>(NativeArray<T> nativeArray, BatchStrategy batchStrategy)
            where T : unmanaged
        {
            Debug_EnsureNoData();
            Debug_EnsureNoScheduleInfo();
            m_ScheduleInfo = new NativeArrayScheduleInfo<T>(nativeArray, batchStrategy);
            return this;
        }

        public IJobConfig ScheduleOn(EntityQuery entityQuery, BatchStrategy batchStrategy)
        {
            Debug_EnsureNoData();
            Debug_EnsureNoScheduleInfo();
            m_ScheduleInfo = new EntityQueryScheduleInfo(entityQuery, batchStrategy);
            return this;
        }

        //TODO: Add in ScheduleOn for Query Components

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DATA STREAM
        //*************************************************************************************************************

        public IJobConfig RequireDataStreamForUpdate(AbstractProxyDataStream dataStream)
        {
            Debug_EnsureScheduleInfoExists();
            
            JobConfigDataID id = new JobConfigDataID(dataStream, Usage.Update);
            
            Debug_EnsureWrapperValidity(id);
            Debug_EnsureWrapperUsage(id);
            
            m_AccessWrappers.Add(id,
                                 new DataStreamAccessWrapper(dataStream, AccessType.ExclusiveWrite));
            return this;
        }

        public IJobConfig RequireDataStreamForWrite(AbstractProxyDataStream dataStream)
        {
            Debug_EnsureScheduleInfoExists();

            JobConfigDataID id = new JobConfigDataID(dataStream, Usage.Write);
            
            Debug_EnsureWrapperValidity(id);
            Debug_EnsureWrapperUsage(id);
            
            m_AccessWrappers.Add(id,
                                 new DataStreamAccessWrapper(dataStream, AccessType.SharedWrite));
            return this;
        }

        public IJobConfig RequireDataStreamForRead(AbstractProxyDataStream dataStream)
        {
            Debug_EnsureScheduleInfoExists();

            JobConfigDataID id = new JobConfigDataID(dataStream, Usage.Read);
            
            Debug_EnsureWrapperValidity(id);
            Debug_EnsureWrapperUsage(id);
            
            m_AccessWrappers.Add(id,
                                 new DataStreamAccessWrapper(dataStream, AccessType.SharedRead));
            return this;
        }

        public IJobConfig RequireResolveChannel<TResolveChannel>(TResolveChannel resolveChannel)
            where TResolveChannel : Enum
        {
            Debug_EnsureScheduleInfoExists();

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
        public IJobConfig RequireNativeArrayForWrite<T>(NativeArray<T> array)
            where T : unmanaged
        {
            Debug_EnsureScheduleInfoExists();

            JobConfigDataID id = new JobConfigDataID(typeof(NativeArray<T>), Usage.Write);
            
            Debug_EnsureWrapperValidity(id);
            
            m_AccessWrappers.Add(id,
                                 NativeArrayAccessWrapper.Create(array));
            return this;
        }
        
        public IJobConfig RequireNativeArrayForRead<T>(NativeArray<T> array)
            where T : unmanaged
        {
            Debug_EnsureScheduleInfoExists();

            JobConfigDataID id = new JobConfigDataID(typeof(NativeArray<T>), Usage.Read);
            
            Debug_EnsureWrapperValidity(id);
            
            m_AccessWrappers.Add(id,
                                 NativeArrayAccessWrapper.Create(array));
            return this;
        }
        
        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - ENTITY QUERY
        //*************************************************************************************************************
        
        public IJobConfig RequireEntityNativeArrayFromQueryForRead(EntityQuery entityQuery)
        {
            Debug_EnsureScheduleInfoExists();

            JobConfigDataID id = new JobConfigDataID(typeof(EntityQueryAccessWrapper.EntityQueryType<Entity>), Usage.Read);
            
            Debug_EnsureWrapperValidity(id);
            
            EntityQueryAccessWrapper wrapper = new EntityQueryAccessWrapper(entityQuery);
            
            m_AccessWrappers.Add(id, wrapper);

            if (m_ScheduleInfo is EntityQueryScheduleInfo entityQueryScheduleInfo)
            {
                entityQueryScheduleInfo.LinkWithWrapper(wrapper);
            }
            
            return this;
        }

        //*************************************************************************************************************
        // EXECUTION
        //*************************************************************************************************************
        
        private JobHandle PrepareAndSchedule(JobHandle dependsOn)
        {
            Debug_EnsureScheduleInfoExists();

            //TODO: Harden this so we can get optimized structures
            foreach (IAccessWrapper wrapper in m_AccessWrappers.Values)
            {
                //TODO: Convert to native array
                dependsOn = JobHandle.CombineDependencies(dependsOn, wrapper.Acquire());
            }

            dependsOn = m_ScheduleJobFunction(dependsOn, m_JobData, m_ScheduleInfo);
            
            //TODO: Use optimized structure
            foreach (IAccessWrapper wrapper in m_AccessWrappers.Values)
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

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNoData()
        {
            if (m_AccessWrappers.Count > 0)
            {
                throw new InvalidOperationException($"{this} has required data specified but {nameof(ScheduleOn)} wasn't called first! This shouldn't happen due to interfaces but perhaps code changes invalidated this?");
            }
        }
        
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNoScheduleInfo()
        {
            if (m_ScheduleInfo != null)
            {
                throw new InvalidOperationException($"{this} is trying to schedule a job but it already has Schedule Info {m_ScheduleInfo} defined! Only call {nameof(ScheduleOn)} once!");
            }
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureScheduleInfoExists()
        {
            if (m_ScheduleInfo == null)
            {
                throw new InvalidOperationException($"{this} does not have a {nameof(IScheduleInfo)} yet! Please call {nameof(ScheduleOn)} first.");
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
        private void Debug_EnsureWrapperUsage(JobConfigDataID id)
        {
            //Access checks
            switch (id.Usage)
            {
                case Usage.Update:
                    Debug_EnsureWrapperUsageValid(id);
                    break;
                case Usage.Write:
                    Debug_EnsureWrapperUsageValid(id, Usage.Read);
                    break;
                case Usage.Read:
                    Debug_EnsureWrapperUsageValid(id, Usage.Write);
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
        
        
        //TODO: See where this can fit during hardening
        // [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        // private void Debug_EnsureDataStreamIntegrity(AbstractProxyDataStream dataStream, Type expectedType, ITaskDriver taskDriver)
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
