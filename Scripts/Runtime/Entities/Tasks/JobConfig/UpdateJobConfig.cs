using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class UpdateJobConfig<TInstance> : AbstractJobConfig,
                                                IUpdateJobConfigRequirements
        where TInstance : unmanaged, IProxyInstance
    {
        private readonly JobConfigDelegates.ScheduleUpdateJobDelegate<TInstance> m_ScheduleJobFunction;
        private readonly UpdateTaskStreamScheduleInfo<TInstance> m_ScheduleInfo;
        private readonly JobResolveTargetMapping m_JobResolveTargetMapping;

        private DataStreamTargetResolver m_DataStreamTargetResolver;

        public UpdateJobConfig(TaskFlowGraph taskFlowGraph,
                               ITaskSystem taskSystem,
                               ITaskDriver taskDriver,
                               JobConfigDelegates.ScheduleUpdateJobDelegate<TInstance> scheduleJobFunction,
                               ITaskStream<TInstance> taskStream,
                               BatchStrategy batchStrategy,
                               CancelRequestsDataStream cancelRequestsDataStream) : base(taskFlowGraph, taskSystem, taskDriver)
        {
            m_ScheduleJobFunction = scheduleJobFunction;
            ScheduleInfo = m_ScheduleInfo = new UpdateTaskStreamScheduleInfo<TInstance>(taskStream.DataStream, batchStrategy);
            m_JobResolveTargetMapping = new JobResolveTargetMapping();

            RequireDataStreamForUpdate(taskStream, cancelRequestsDataStream);
        }

        protected override void DisposeSelf()
        {
            m_DataStreamTargetResolver.Dispose();

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

        public IUpdateJobConfigRequirements RequireResolveTarget<TResolveTarget>(TResolveTarget resolveTarget)
            where TResolveTarget : Enum
        {
            ResolveTargetUtil.Debug_EnsureEnumValidity(resolveTarget);

            //Any data streams that have registered for this resolve target type either on the system or related task drivers will be needed.
            //When the updater runs, it doesn't know yet which resolve target a particular instance will resolve to yet until it actually resolves.
            //We need to ensure that all possible locations have write access
            TaskFlowGraph.PopulateJobResolveTargetMappingForChannel(resolveTarget, m_JobResolveTargetMapping, TaskSystem);

            IEnumerable<ResolveTargetData> resolveTargetData = m_JobResolveTargetMapping.GetResolveTargetData(resolveTarget);
            foreach (ResolveTargetData data in resolveTargetData)
            {
                AddAccessWrapper(new JobConfigDataID(data.DataStream, Usage.Write),
                                 DataStreamAsResolveTargetAccessWrapper.Create(resolveTarget, data));
            }

            return this;
        }

        //*************************************************************************************************************
        // HARDEN
        //*************************************************************************************************************

        protected sealed override void HardenConfig()
        {
            m_DataStreamTargetResolver = new DataStreamTargetResolver(m_JobResolveTargetMapping);
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

        internal override DataStreamTargetResolver GetDataStreamChannelResolver()
        {
            return m_DataStreamTargetResolver;
        }
    }
}
