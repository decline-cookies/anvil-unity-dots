using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class TaskDriverCancelFlow : AbstractCancelFlow
    {
        internal static readonly BulkScheduleDelegate<TaskDriverCancelFlow> SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<TaskDriverCancelFlow>(nameof(Schedule), BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly AbstractTaskDriver m_TaskDriver;
        private readonly SystemCancelFlow m_SystemCancelFlow;
        private readonly Dictionary<int, List<AbstractCancelFlow>> m_CancelFlowHierarchy;
        private BulkJobScheduler<AbstractCancelFlow>[] m_OrderedBulkJobSchedulers;


        //Request data
        private readonly List<CancelRequestDataStream> m_CancelRequestDataStreams;
        private NativeArray<byte> m_RequestContexts;
        private NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.Writer> m_RequestWriters;
        private NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter> m_RequestLaneWriters;
        private NativeArray<JobHandle> m_CancelRequestAcquisitionJobHandles;

        public byte TaskDriverContext
        {
            get => m_TaskDriver.Context;
        }

        public TaskDriverCancelFlow(AbstractTaskDriver taskDriver, TaskDriverCancelFlow parent) : base(taskDriver.TaskData, parent)
        {
            m_TaskDriver = taskDriver;
            m_SystemCancelFlow = new SystemCancelFlow(m_TaskDriver.TaskSystem, this);
            m_CancelFlowHierarchy = new Dictionary<int, List<AbstractCancelFlow>>();
            m_CancelRequestDataStreams = new List<CancelRequestDataStream>();
        }

        protected override void DisposeSelf()
        {
            if (m_RequestContexts.IsCreated)
            {
                m_RequestContexts.Dispose();
            }

            if (m_RequestWriters.IsCreated)
            {
                m_RequestWriters.Dispose();
            }

            if (m_RequestLaneWriters.IsCreated)
            {
                m_RequestLaneWriters.Dispose();
            }

            if (m_CancelRequestAcquisitionJobHandles.IsCreated)
            {
                m_CancelRequestAcquisitionJobHandles.Dispose();
            }

            if (m_OrderedBulkJobSchedulers != null)
            {
                foreach (BulkJobScheduler<AbstractCancelFlow> bulkJobScheduler in m_OrderedBulkJobSchedulers)
                {
                    bulkJobScheduler.Dispose();
                }
            }

            m_SystemCancelFlow.Dispose();
            base.DisposeSelf();
        }

        internal CancelRequestsWriter CreateCancelRequestsWriter()
        {
            return new CancelRequestsWriter(m_RequestWriters,
                                            m_RequestLaneWriters,
                                            m_RequestContexts);
        }

        public void BuildRequestData()
        {
            List<byte> cancelContexts = new List<byte>();
            List<AbstractCancelFlow> cancelFlows = new List<AbstractCancelFlow>();
            BuildRequestData(cancelFlows, m_CancelRequestDataStreams, cancelContexts);

            m_RequestWriters = new NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.Writer>(m_CancelRequestDataStreams.Count, Allocator.Persistent);
            m_RequestLaneWriters = new NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter>(m_CancelRequestDataStreams.Count, Allocator.Persistent);
            m_RequestContexts = new NativeArray<byte>(m_CancelRequestDataStreams.Count, Allocator.Persistent);

            for (int i = 0; i < m_RequestWriters.Length; ++i)
            {
                m_RequestWriters[i] = m_CancelRequestDataStreams[i].Pending.AsWriter();
                m_RequestContexts[i] = cancelContexts[i];
            }

            m_CancelRequestAcquisitionJobHandles = new NativeArray<JobHandle>(m_CancelRequestDataStreams.Count, Allocator.Persistent);
        }

        private void BuildRequestData(List<AbstractCancelFlow> cancelFlows,
                                      List<CancelRequestDataStream> cancelRequests,
                                      List<byte> contexts)
        {
            cancelFlows.Add(this);
            //Add ourself
            cancelRequests.Add(TaskData.CancelRequestDataStream);
            //Add our TaskDriver's context
            contexts.Add(TaskDriverContext);

            //For all subtask drivers, recursively add
            foreach (AbstractTaskDriver taskDriver in m_TaskDriver.SubTaskDrivers)
            {
                taskDriver.CancelFlow.BuildRequestData(cancelFlows, cancelRequests, contexts);
            }

            //Add our governing system
            m_SystemCancelFlow.BuildRelationshipData(cancelFlows, cancelRequests, contexts);
        }

        public void BuildScheduling()
        {
            //Build up the hierarchy of when things should be scheduled so that the bottom most get a chance first
            //This will ensure the order of jobs executed to allow for possible 1 frame bubble up of completes
            BuildSchedulingHierarchy(m_TaskDriver, 0);

            int maxDepth = m_CancelFlowHierarchy.Count - 1;

            List<BulkJobScheduler<AbstractCancelFlow>> orderedBulkJobSchedulers = new List<BulkJobScheduler<AbstractCancelFlow>>();

            for (int depth = maxDepth; depth >= 0; --depth)
            {
                List<AbstractCancelFlow> cancelFlowsAtDepth = m_CancelFlowHierarchy[depth];
                BulkJobScheduler<AbstractCancelFlow> bulkJobScheduler = new BulkJobScheduler<AbstractCancelFlow>(cancelFlowsAtDepth.ToArray());
                orderedBulkJobSchedulers.Add(bulkJobScheduler);
            }

            m_OrderedBulkJobSchedulers = orderedBulkJobSchedulers.ToArray();
        }

        private void BuildSchedulingHierarchy(AbstractTaskDriver taskDriver, int depth)
        {
            List<AbstractCancelFlow> cancelFlows = GetOrCreateAtDepth(depth);
            List<AbstractCancelFlow> cancelFlowsOneDeeper = GetOrCreateAtDepth(depth + 1);

            //Add our own Cancel Flow
            cancelFlows.Add(taskDriver.CancelFlow);
            //Add the System's Cancel Flow to the next depth
            cancelFlowsOneDeeper.Add(taskDriver.CancelFlow.m_SystemCancelFlow);

            //Drill down into the children
            foreach (AbstractTaskDriver subTaskDriver in taskDriver.SubTaskDrivers)
            {
                BuildSchedulingHierarchy(subTaskDriver, depth + 1);
            }
        }

        private List<AbstractCancelFlow> GetOrCreateAtDepth(int depth)
        {
            if (!m_CancelFlowHierarchy.TryGetValue(depth, out List<AbstractCancelFlow> cancelFlows))
            {
                cancelFlows = new List<AbstractCancelFlow>();
                m_CancelFlowHierarchy.Add(depth, cancelFlows);
            }

            return cancelFlows;
        }


        private JobHandle Schedule(JobHandle dependsOn)
        {
            int len = m_OrderedBulkJobSchedulers.Length;
            if (len == 0)
            {
                return dependsOn;
            }

            for (int i = 0; i < len; ++i)
            {
                dependsOn = ScheduleBulkSchedulers(m_OrderedBulkJobSchedulers[i], dependsOn);
            }

            return dependsOn;
        }

        private JobHandle ScheduleBulkSchedulers(BulkJobScheduler<AbstractCancelFlow> bulkJobScheduler, JobHandle dependsOn)
        {
            return bulkJobScheduler.Schedule(dependsOn, CHECK_PROGRESS_SCHEDULE_FUNCTION);
        }

        public JobHandle AcquireAsync(AccessType accessType)
        {
            for (int i = 0; i < m_CancelRequestDataStreams.Count; ++i)
            {
                m_CancelRequestAcquisitionJobHandles[i] = m_CancelRequestDataStreams[i].AccessController.AcquireAsync(accessType);
            }
            return JobHandle.CombineDependencies(m_CancelRequestAcquisitionJobHandles);
        }

        public void ReleaseAsync(JobHandle releaseAccessDependency)
        {
            foreach (CancelRequestDataStream cancelRequestDataStream in m_CancelRequestDataStreams)
            {
                cancelRequestDataStream.AccessController.ReleaseAsync(releaseAccessDependency);
            }
        }
    }
}
