using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelRequestsDataStream : AbstractDataStream
    {
        internal static readonly BulkScheduleDelegate<CancelRequestsDataStream> CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<CancelRequestsDataStream>(nameof(ConsolidateForFrame), BindingFlags.Instance | BindingFlags.NonPublic);
        
        private static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<EntityProxyInstanceID>();

        private UnsafeTypedStream<EntityProxyInstanceID> m_Trigger;
        private UnsafeTypedStream<EntityProxyInstanceID> m_Pending;
        private UnsafeParallelHashMap<EntityProxyInstanceID, byte> m_Lookup;

        public CancelRequestsDataStream()
        {
            m_Trigger = new UnsafeTypedStream<EntityProxyInstanceID>(Allocator.Persistent);
            m_Pending = new UnsafeTypedStream<EntityProxyInstanceID>(Allocator.Persistent);
            m_Lookup = new UnsafeParallelHashMap<EntityProxyInstanceID, byte>(MAX_ELEMENTS_PER_CHUNK, Allocator.Persistent);
        }

        protected override void DisposeSelf()
        {
            AccessController.Acquire(AccessType.Disposal);
            m_Trigger.Dispose();
            m_Pending.Dispose();
            m_Lookup.Dispose();


            base.DisposeSelf();
        }

        //*************************************************************************************************************
        // SERIALIZATION
        //*************************************************************************************************************

        //TODO: #83 - Add support for Serialization. Hopefully from the outside or via extension methods instead of functions
        //here but keeping the TODO for future reminder.

        //*************************************************************************************************************
        // JOB STRUCTS
        //*************************************************************************************************************

        internal CancelRequestsReader CreateCancelRequestsReader()
        {
            return new CancelRequestsReader(m_Lookup);
        }

        internal CancelRequestsWriter CreateCancelRequestsWriter(byte context)
        {
            return new CancelRequestsWriter(m_Trigger.AsWriter(), context);
        }

        //*************************************************************************************************************
        // CONSOLIDATION
        //*************************************************************************************************************

        private JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            JobHandle exclusiveWriteHandle = AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            ConsolidateCancelRequestsJob consolidateCancelRequestsJob = new ConsolidateCancelRequestsJob(m_Pending,
                                                                                                         m_Lookup);
            JobHandle consolidateHandle = consolidateCancelRequestsJob.Schedule(JobHandle.CombineDependencies(dependsOn, exclusiveWriteHandle));

            AccessController.ReleaseAsync(consolidateHandle);

            return consolidateHandle;
        }
        
        internal ref UnsafeTypedStream<EntityProxyInstanceID> TriggerRef
        {
            get => ref m_Trigger;
        }

        internal ref UnsafeTypedStream<EntityProxyInstanceID> PendingRef
        {
            get => ref m_Pending;
        }

        internal ref UnsafeParallelHashMap<EntityProxyInstanceID, byte> LookupRef
        {
            get => ref m_Lookup;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ConsolidateCancelRequestsJob : IJob
        {
            [ReadOnly] private UnsafeTypedStream<EntityProxyInstanceID> m_Pending;
            private UnsafeParallelHashMap<EntityProxyInstanceID, byte> m_Lookup;

            public ConsolidateCancelRequestsJob(UnsafeTypedStream<EntityProxyInstanceID> pending,
                                                UnsafeParallelHashMap<EntityProxyInstanceID, byte> lookup)
            {
                m_Pending = pending;
                m_Lookup = lookup;
            }

            public void Execute()
            {
                m_Lookup.Clear();
                
                foreach (EntityProxyInstanceID proxyInstanceID in m_Pending)
                {
                    Debug_EnsureNoDuplicates(proxyInstanceID);
                    m_Lookup.TryAdd(proxyInstanceID, 1);
                }
                
                m_Pending.Clear();
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
