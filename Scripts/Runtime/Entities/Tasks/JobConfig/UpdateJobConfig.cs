using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class UpdateJobConfig<TInstance> : AbstractJobConfig,
                                                IUpdateJobConfig,
                                                IUpdateJobConfigRequirements
        where TInstance : unmanaged, IProxyInstance
    {
        private readonly IUpdateJobConfig.ScheduleJobDelegate<TInstance> m_ScheduleJobFunction;
        private readonly UpdateTaskStreamScheduleInfo<TInstance> m_ScheduleInfo;
        private readonly JobResolveChannelMapping m_JobResolveChannelMapping;

        private DataStreamChannelResolver m_DataStreamChannelResolver;

        public UpdateJobConfig(TaskFlowGraph taskFlowGraph,
                               ITaskSystem taskSystem,
                               ITaskDriver taskDriver,
                               IUpdateJobConfig.ScheduleJobDelegate<TInstance> scheduleJobFunction,
                               ITaskStream<TInstance> taskStream,
                               BatchStrategy batchStrategy,
                               CancelRequestsDataStream cancelRequestsDataStream) : base(taskFlowGraph, taskSystem, taskDriver)
        {
            m_ScheduleJobFunction = scheduleJobFunction;
            ScheduleInfo = m_ScheduleInfo = new UpdateTaskStreamScheduleInfo<TInstance>(taskStream.DataStream, batchStrategy);
            m_JobResolveChannelMapping = new JobResolveChannelMapping();

            RequireDataStreamForUpdate(taskStream, cancelRequestsDataStream);
        }

        protected override void DisposeSelf()
        {
            m_DataStreamChannelResolver.Dispose();

            base.DisposeSelf();
        }

        protected sealed override string GetScheduleJobFunctionDebugInfo()
        {
            return $"{m_ScheduleJobFunction.Method.DeclaringType?.Name}.{m_ScheduleJobFunction.Method.Name}";
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - CANCELLATION
        //*************************************************************************************************************

        private void RequireRequestCancelDataStreamForRead(CancelRequestsDataStream cancelRequestsDataStream)
        {
            AddAccessWrapper(new JobConfigDataID(cancelRequestsDataStream, Usage.Read),
                             new DataStreamAccessWrapper(cancelRequestsDataStream, AccessType.SharedRead));
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DATA STREAM
        //*************************************************************************************************************

        private void RequireDataStreamForUpdate(ITaskStream<TInstance> taskStream, CancelRequestsDataStream cancelRequestsDataStream)
        {
            AddAccessWrapper(new JobConfigDataID(taskStream.DataStream, Usage.Update),
                             new DataStreamAccessWrapper(taskStream.DataStream, AccessType.ExclusiveWrite));
            
            RequireRequestCancelDataStreamForRead(cancelRequestsDataStream);

            if (taskStream is not CancellableTaskStream<TInstance> cancellableTaskStream)
            {
                return;
            }

            RequireDataStreamForWrite(cancellableTaskStream.PendingCancelDataStream, Usage.WritePendingCancel);
        }

        public IUpdateJobConfigRequirements RequireResolveChannel<TResolveChannel>(TResolveChannel resolveChannel)
            where TResolveChannel : Enum
        {
            ResolveChannelUtil.Debug_EnsureEnumValidity(resolveChannel);

            //Any data streams that have registered for this resolve channel type either on the system or related task drivers will be needed.
            //When the updater runs, it doesn't know yet which resolve channel a particular instance will resolve to yet until it actually resolves.
            //We need to ensure that all possible locations have write access
            TaskFlowGraph.PopulateJobResolveChannelMappingForChannel(resolveChannel, m_JobResolveChannelMapping, TaskSystem);

            IEnumerable<ResolveChannelData> resolveChannelData = m_JobResolveChannelMapping.GetResolveChannelData(resolveChannel);
            foreach (ResolveChannelData data in resolveChannelData)
            {
                AddAccessWrapper(new JobConfigDataID(data.DataStream, Usage.Write),
                                 DataStreamAsResolveChannelAccessWrapper.Create(resolveChannel, data));
            }

            return this;
        }

        //*************************************************************************************************************
        // HARDEN
        //*************************************************************************************************************

        protected sealed override void HardenConfig()
        {
            m_DataStreamChannelResolver = new DataStreamChannelResolver(m_JobResolveChannelMapping);
        }

        //*************************************************************************************************************
        // EXECUTION
        //*************************************************************************************************************

        protected sealed override JobHandle CallScheduleFunction(JobHandle dependsOn,
                                                                 JobData jobData)
        {
            CancelRequestsReader cancelRequestsReader = jobData.GetRequestCancelReader();
            m_ScheduleInfo.Updater = jobData.GetDataStreamUpdater<TInstance>(cancelRequestsReader);
            return m_ScheduleJobFunction(dependsOn, jobData, m_ScheduleInfo);
        }

        internal override DataStreamChannelResolver GetDataStreamChannelResolver()
        {
            return m_DataStreamChannelResolver;
        }
    }
}
