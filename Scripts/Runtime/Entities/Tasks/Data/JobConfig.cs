using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public class JobConfig
    {
        internal enum Usage
        {
            Update,
            Write,
            Read
        }

        internal static readonly BulkScheduleDelegate<JobConfig> PREPARE_AND_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<JobConfig>(nameof(PrepareAndSchedule), BindingFlags.Instance | BindingFlags.NonPublic);

        public delegate JobHandle ScheduleJobDelegate(JobHandle jobHandle, JobData jobData, IScheduleInfo scheduleInfo);


        private readonly ScheduleJobDelegate m_ScheduleJobFunction;
        private readonly Dictionary<DataStreamJobConfigID, DataStreamAccessWrapper> m_DataStreamAccessWrappers;
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
            m_DataStreamAccessWrappers = new Dictionary<DataStreamJobConfigID, DataStreamAccessWrapper>();
            m_JobData = new JobData(m_TaskSystem.World,
                                    m_TaskDriver?.Context ?? m_TaskSystem.Context,
                                    this);
        }

        //TODO: Cross reference with JobTaskWorkConfig to include safety checks and other data
        protected JobHandle PrepareAndSchedule(JobHandle dependsOn)
        {
            //TODO Harden this so we can get optimized structures
            foreach (DataStreamAccessWrapper wrapper in m_DataStreamAccessWrappers.Values)
            {
                //TODO: Convert to native array
                dependsOn = JobHandle.CombineDependencies(dependsOn, wrapper.DataStream.AccessController.AcquireAsync(wrapper.AccessType));
            }

            dependsOn = m_ScheduleJobFunction(dependsOn, m_JobData, m_ScheduleInfo);
            
            foreach (DataStreamAccessWrapper wrapper in m_DataStreamAccessWrappers.Values)
            {
                wrapper.DataStream.AccessController.ReleaseAsync(dependsOn);
            }

            return dependsOn;
        }

        public JobConfig ScheduleOn<TInstance>(ProxyDataStream<TInstance> dataStream, BatchStrategy batchStrategy)
            where TInstance : unmanaged, IProxyInstance
        {
            // TODO: Debug_EnsureNoDuplicateScheduleInfo();
            m_ScheduleInfo = new ProxyDataStreamScheduleInfo<TInstance>(dataStream, batchStrategy);
            return this;
        }

        //TODO: Add in ScheduleOn for Query
        //TODO: Add in ScheduleOn for NativeArray


        //TODO: Want to store the wrappers as a Type and Context

        public JobConfig RequireDataForUpdate(AbstractProxyDataStream dataStream)
        {
            //TODO: Ensure doesn't already exist in dictionary
            //TODO: Ensure type of dataStream matches the config type
            m_DataStreamAccessWrappers.Add(new DataStreamJobConfigID(dataStream, Usage.Update),
                                           new DataStreamAccessWrapper(dataStream, AccessType.ExclusiveWrite));
            return this;
        }

        public JobConfig RequireDataForWrite(AbstractProxyDataStream dataStream)
        {
            //TODO: Ensure doesn't already exist in dictionary
            //TODO: Ensure type of dataStream matches the config type
            m_DataStreamAccessWrappers.Add(new DataStreamJobConfigID(dataStream, Usage.Write),
                                           new DataStreamAccessWrapper(dataStream, AccessType.SharedWrite));
            return this;
        }

        public JobConfig RequireDataForRead(AbstractProxyDataStream dataStream)
        {
            //TODO: Ensure doesn't already exist in dictionary
            //TODO: Ensure type of dataStream matches the config type
            m_DataStreamAccessWrappers.Add(new DataStreamJobConfigID(dataStream, Usage.Read),
                                           new DataStreamAccessWrapper(dataStream, AccessType.SharedRead));
            return this;
        }

        public JobConfig RequireResolveChannelData<TResolveChannel>(TResolveChannel resolveChannel)
            where TResolveChannel : Enum
        {
            //TODO: This means any data on our task drivers or ourself that matches this resultType needs to be included
            List<AbstractProxyDataStream> dataStreams = m_TaskFlowGraph.GetResolveChannelDataStreams(resolveChannel, m_TaskSystem, m_TaskDriver);
            foreach (AbstractProxyDataStream dataStream in dataStreams)
            {
                RequireDataForWrite(dataStream);
            }

            return this;
        }


        internal ProxyDataStream<TInstance> GetDataStream<TInstance>(Usage usage)
            where TInstance : unmanaged, IProxyInstance
        {
            DataStreamJobConfigID id = new DataStreamJobConfigID(typeof(ProxyDataStream<TInstance>), usage);
            //TODO: Ensure wrapper exists
            return (ProxyDataStream<TInstance>)m_DataStreamAccessWrappers[id].DataStream;
        }
    }
}
