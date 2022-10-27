using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System.Collections.Generic;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    //Helper class to manage writing cancellation requests (which come in from one TaskDriver) to that TaskDriver's 
    //TaskSystem and to all sub-TaskDrivers in one quick job.
    internal class TaskDriverCancellationPropagator : AbstractAnvilBase
    {
        internal static readonly BulkScheduleDelegate<TaskDriverCancellationPropagator> CONSOLIDATE_AND_PROPAGATE_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<TaskDriverCancellationPropagator>(nameof(ConsolidateAndPropagate), BindingFlags.Instance | BindingFlags.NonPublic);
    
        private readonly CancelRequestsDataStream m_TaskDriverCancelRequestsDataStream;
        
        
        private readonly List<CancelRequestsDataStream> m_PropagatedCancelRequestsDataStreams;
        private NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.Writer> m_CancelRequestsWriters;
        private NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter> m_CancelRequestsLaneWriters;

        private readonly int m_TaskDriverDependencyIndex;
        private readonly int m_IncomingDependencyIndex;
        private readonly FixedString64Bytes m_TaskDriverName;
        
        
        private NativeArray<JobHandle> m_Dependencies;

        public AbstractTaskDriver TaskDriver { get; }

        public TaskDriverCancellationPropagator(AbstractTaskDriver taskDriver)
        {
            TaskDriver = taskDriver;
            m_TaskDriverName = new FixedString64Bytes(TaskDriver.ToString());
            m_TaskDriverCancelRequestsDataStream = taskDriver.CancelRequestsDataStream;
            
            
            m_PropagatedCancelRequestsDataStreams = new List<CancelRequestsDataStream>();
            taskDriver.AddCancelRequestsTo(m_PropagatedCancelRequestsDataStreams);
            
            m_CancelRequestsWriters = new NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.Writer>(m_PropagatedCancelRequestsDataStreams.Count, Allocator.Persistent);
            m_CancelRequestsLaneWriters = new NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter>(m_CancelRequestsWriters.Length, Allocator.Persistent);
            for (int i = 0; i < m_CancelRequestsWriters.Length; ++i)
            {
                m_CancelRequestsWriters[i] = m_PropagatedCancelRequestsDataStreams[i].PendingRef.AsWriter();
            }

            int numSubDrivers = m_PropagatedCancelRequestsDataStreams.Count;
            
            m_Dependencies = new NativeArray<JobHandle>(numSubDrivers + 2, Allocator.Persistent);
            m_TaskDriverDependencyIndex = numSubDrivers;
            m_IncomingDependencyIndex = numSubDrivers + 1;
        }

        protected override void DisposeSelf()
        {
            if (m_Dependencies.IsCreated)
            {
                m_Dependencies.Dispose();
            }

            if (m_CancelRequestsWriters.IsCreated)
            {
                m_CancelRequestsWriters.Dispose();
            }

            if (m_CancelRequestsLaneWriters.IsCreated)
            {
                m_CancelRequestsLaneWriters.Dispose();
            }
            base.DisposeSelf();
        }

        private JobHandle ConsolidateAndPropagate(JobHandle dependsOn)
        {
            for (int i = 0; i < m_PropagatedCancelRequestsDataStreams.Count; ++i)
            {
                m_Dependencies[i] = m_PropagatedCancelRequestsDataStreams[i].AccessController.AcquireAsync(AccessType.SharedWrite);
            }

            //TODO: Change this once we've reworked to handle the Trigger and Pending propagation
            // m_Dependencies[m_TaskDriverDependencyIndex] = m_TaskDriverCancelRequestsDataStream.AccessController.AcquireAsync(AccessType.SharedRead);
            m_Dependencies[m_TaskDriverDependencyIndex] = default;
            m_Dependencies[m_IncomingDependencyIndex] = dependsOn;
            
            ConsolidateAndPropagateCancelRequestsJob job = new ConsolidateAndPropagateCancelRequestsJob(ref m_TaskDriverCancelRequestsDataStream.TriggerRef,
                                                                                                        m_CancelRequestsWriters,
                                                                                                        m_CancelRequestsLaneWriters,
                                                                                                        m_TaskDriverName);
            dependsOn = job.Schedule(JobHandle.CombineDependencies(m_Dependencies));

            foreach (CancelRequestsDataStream cancelStream in m_PropagatedCancelRequestsDataStreams)
            {
                cancelStream.AccessController.ReleaseAsync(dependsOn);
            }
            //TODO: Change this once we've reworked to handle the Trigger and Pending propagation
            // m_TaskDriverCancelRequestsDataStream.AccessController.ReleaseAsync(dependsOn);
            
            return dependsOn;
        }
        
        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************
        
        [BurstCompile]
        private struct ConsolidateAndPropagateCancelRequestsJob : IJob
        {
            private const int UNSET_NATIVE_THREAD_INDEX = -1;
            
            [ReadOnly] private UnsafeTypedStream<EntityProxyInstanceID> m_Trigger;

            private NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.Writer> m_PendingSubTaskDriverWriters;
            private NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter> m_PendingSubTaskDriverLaneWriters;

            [NativeSetThreadIndex] private readonly int m_NativeThreadIndex;
            
            //TODO: REMOVE
            private readonly FixedString64Bytes m_TaskDriverName;

            public ConsolidateAndPropagateCancelRequestsJob(ref UnsafeTypedStream<EntityProxyInstanceID> trigger,
                                                            NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.Writer> pendingSubTaskDriverWriters,
                                                            NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter> pendingSubTaskDriverLaneWriters,
                                                            FixedString64Bytes taskDriverName)
            {
                m_Trigger = trigger;
                m_PendingSubTaskDriverWriters = pendingSubTaskDriverWriters;
                m_PendingSubTaskDriverLaneWriters = pendingSubTaskDriverLaneWriters;
                
                m_NativeThreadIndex = UNSET_NATIVE_THREAD_INDEX;
                
                m_TaskDriverName = taskDriverName;
            }

            public void Execute()
            {
                int laneIndex = ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex);
                
                for (int i = 0; i < m_PendingSubTaskDriverWriters.Length; ++i)
                {
                    m_PendingSubTaskDriverLaneWriters[i] = m_PendingSubTaskDriverWriters[i].AsLaneWriter(laneIndex);
                }

                if (m_Trigger.Count() > 0)
                {
                    //UnityEngine.Debug.Log($"Propagating Cancel for {m_TaskDriverName}");
                }

                foreach (EntityProxyInstanceID proxyInstanceID in m_Trigger)
                {
                    for (int i = 0; i < m_PendingSubTaskDriverLaneWriters.Length; ++i)
                    {
                        m_PendingSubTaskDriverLaneWriters[i].Write(proxyInstanceID);
                    }
                }

                m_Trigger.Clear();
            }
        }
    }
}
