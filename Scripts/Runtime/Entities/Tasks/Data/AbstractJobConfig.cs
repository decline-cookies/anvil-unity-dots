using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract class AbstractJobConfig
    {
        internal static readonly BulkScheduleDelegate<AbstractJobConfig> PREPARE_AND_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<AbstractJobConfig>(nameof(PrepareAndSchedule), BindingFlags.Instance | BindingFlags.NonPublic);
        
        private readonly Dictionary<AbstractProxyDataStream, DataStreamAccessWrapper> m_DataStreamAccessWrappers;
        private readonly ITaskFlowGraph m_TaskFlowGraph;
        private readonly ITaskSystem m_TaskSystem;
        private readonly ITaskDriver m_TaskDriver;

        private IScheduleInfo m_ScheduleInfo;

        protected AbstractJobConfig(ITaskFlowGraph taskFlowGraph, ITaskSystem taskSystem, ITaskDriver taskDriver)
        {
            m_TaskFlowGraph = taskFlowGraph;
            m_TaskSystem = taskSystem;
            m_TaskDriver = taskDriver;
            m_DataStreamAccessWrappers = new Dictionary<AbstractProxyDataStream, DataStreamAccessWrapper>();
        }

        protected abstract JobHandle PrepareAndSchedule(JobHandle dependsOn);

        public AbstractJobConfig ScheduleOn<TInstance>(ProxyDataStream<TInstance> dataStream, BatchStrategy batchStrategy)
            where TInstance : unmanaged, IProxyInstance
        {
            // TODO: Debug_EnsureNoDuplicateScheduleInfo();
            m_ScheduleInfo = new ProxyDataStreamScheduleInfo<TInstance>(dataStream, batchStrategy);
            return this;
        }
        
        //TODO: Add in ScheduleOn for Query
        //TODO: Add in ScheduleOn for NativeArray
        
        public AbstractJobConfig RequireDataForUpdate(AbstractProxyDataStream dataStream)
        {
            //TODO: Ensure doesn't already exist in dictionary
            //TODO: Ensure type of dataStream matches the config type
            m_DataStreamAccessWrappers.Add(dataStream, new DataStreamAccessWrapper(dataStream, AccessType.ExclusiveWrite));
            return this;
        }
        
        public AbstractJobConfig RequireDataForWrite(AbstractProxyDataStream dataStream)
        {
            //TODO: Ensure doesn't already exist in dictionary
            //TODO: Ensure type of dataStream matches the config type
            m_DataStreamAccessWrappers.Add(dataStream, new DataStreamAccessWrapper(dataStream, AccessType.SharedWrite));
            return this;
        }
        
        public AbstractJobConfig RequireDataForRead(AbstractProxyDataStream dataStream)
        {
            //TODO: Ensure doesn't already exist in dictionary
            //TODO: Ensure type of dataStream matches the config type
            m_DataStreamAccessWrappers.Add(dataStream, new DataStreamAccessWrapper(dataStream, AccessType.SharedRead));
            return this;
        }

        public AbstractJobConfig RequireResolveChannelData<TResolveChannel>(TResolveChannel resolveChannel)
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
    }
}
