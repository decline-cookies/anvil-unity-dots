using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    
        private readonly CancelRequestsDataStream m_TaskDriverCancelRequests;
        
        
        private readonly List<CancelRequestsDataStream> m_CancelRequests;
        private NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.Writer> m_CancelRequestsWriters;
        private NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter> m_CancelRequestsLaneWriters;

        private readonly int m_TaskDriverDependencyIndex;
        private readonly int m_IncomingDependencyIndex;
        
        
        private NativeArray<JobHandle> m_Dependencies;

        public AbstractTaskDriver TaskDriver { get; }

        public TaskDriverCancellationPropagator(AbstractTaskDriver taskDriver)
        {
            TaskDriver = taskDriver;
            m_TaskDriverCancelRequests = taskDriver.CancelRequestsDataStream;
            
            
            m_CancelRequests = new List<CancelRequestsDataStream>();
            m_CancelRequests.Add(taskDriver.TaskSystem.CancelRequestsDataStream);
            taskDriver.AddCancelRequestsTo(m_CancelRequests);
            
            m_CancelRequestsWriters = new NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.Writer>(m_CancelRequests.Count, Allocator.Persistent);
            m_CancelRequestsLaneWriters = new NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter>(m_CancelRequestsWriters.Length, Allocator.Persistent);
            for (int i = 0; i < m_CancelRequestsWriters.Length; ++i)
            {
                m_CancelRequestsWriters[i] = m_CancelRequests[i].PendingRef.AsWriter();
            }

            int numSubDrivers = m_CancelRequests.Count;
            
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
            for (int i = 0; i < m_CancelRequests.Count; ++i)
            {
                m_Dependencies[i] = m_CancelRequests[i].AccessController.AcquireAsync(AccessType.SharedWrite);
            }
            
            m_Dependencies[m_TaskDriverDependencyIndex] = m_TaskDriverCancelRequests.AccessController.AcquireAsync(AccessType.SharedRead);
            m_Dependencies[m_IncomingDependencyIndex] = dependsOn;
            
            ConsolidateAndPropagateCancelRequestsJob job = new ConsolidateAndPropagateCancelRequestsJob(ref m_TaskDriverCancelRequests.TriggerRef,
                                                                                                        ref m_TaskDriverCancelRequests.LookupRef,
                                                                                                        m_CancelRequestsWriters,
                                                                                                        m_CancelRequestsLaneWriters,
                                                                                                        new FixedString64Bytes(TaskDriver.ToString()));
            dependsOn = job.Schedule(JobHandle.CombineDependencies(m_Dependencies));

            foreach (CancelRequestsDataStream cancelStream in m_CancelRequests)
            {
                cancelStream.AccessController.ReleaseAsync(dependsOn);
            }
            m_TaskDriverCancelRequests.AccessController.ReleaseAsync(dependsOn);
            
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
            private UnsafeParallelHashMap<EntityProxyInstanceID, byte> m_Lookup;
            
            private NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.Writer> m_PendingSubTaskDriverWriters;
            private NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter> m_PendingSubTaskDriverLaneWriters;

            [NativeSetThreadIndex] private readonly int m_NativeThreadIndex;
            
            //TODO: REMOVE
            private readonly FixedString64Bytes m_TaskDriverName;

            public ConsolidateAndPropagateCancelRequestsJob(ref UnsafeTypedStream<EntityProxyInstanceID> trigger,
                                                            ref UnsafeParallelHashMap<EntityProxyInstanceID, byte> lookup,
                                                            NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.Writer> pendingSubTaskDriverWriters,
                                                            NativeArray<UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter> pendingSubTaskDriverLaneWriters,
                                                            FixedString64Bytes taskDriverName)
            {
                m_Trigger = trigger;
                m_Lookup = lookup;
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

                m_Lookup.Clear();
                
                if (m_Trigger.Count() > 0)
                {
                    // UnityEngine.Debug.Log($"Propagating Cancel for {m_TaskDriverName}");
                }

                foreach (EntityProxyInstanceID proxyInstanceID in m_Trigger)
                {
                    Debug_EnsureNoDuplicates(proxyInstanceID);
                    m_Lookup.TryAdd(proxyInstanceID, 1);
                    
                    for (int i = 0; i < m_PendingSubTaskDriverLaneWriters.Length; ++i)
                    {
                        m_PendingSubTaskDriverLaneWriters[i].Write(proxyInstanceID);
                    }
                }

                m_Trigger.Clear();
            }
            
            //*************************************************************************************************************
            // SAFETY
            //*************************************************************************************************************

            [Conditional("ANVIL_DEBUG_SAFETY_EXPENSIVE")]
            private void Debug_EnsureNoDuplicates(EntityProxyInstanceID id)
            {
                if (m_Lookup.ContainsKey(id))
                {
                    throw new InvalidOperationException($"Trying to add id of {id} but the same id already exists in the lookup! This should never happen! Investigate.");
                }
            }
        }
    }
}
