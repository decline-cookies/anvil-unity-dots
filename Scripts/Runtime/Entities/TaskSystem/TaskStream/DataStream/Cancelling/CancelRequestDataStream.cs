using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelRequestDataStream : AbstractEntityInstanceIDDataStream
    {
        private static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<EntityProxyInstanceID>();

        internal UnsafeParallelHashMap<EntityProxyInstanceID, byte> Lookup { get; }

        private readonly CancelProgressDataStream m_ProgressDataStream;

        public CancelRequestDataStream(CancelProgressDataStream progressDataStream)
        {
            m_ProgressDataStream = progressDataStream;
            Lookup = new UnsafeParallelHashMap<EntityProxyInstanceID, byte>(MAX_ELEMENTS_PER_CHUNK, Allocator.Persistent);
        }

        protected override void DisposeDataStream()
        {
            Lookup.Dispose();
            base.DisposeDataStream();
        }

        //*************************************************************************************************************
        // SERIALIZATION
        //*************************************************************************************************************

        //TODO: #83 - Add support for Serialization. Hopefully from the outside or via extension methods instead of functions
        //here but keeping the TODO for future reminder.

        //*************************************************************************************************************
        // CONSOLIDATION
        //*************************************************************************************************************

        protected sealed override JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            dependsOn = JobHandle.CombineDependencies(dependsOn,
                                                      AccessController.AcquireAsync(AccessType.ExclusiveWrite),
                                                      m_ProgressDataStream.AccessController.AcquireAsync(AccessType.ExclusiveWrite));

            ConsolidateCancelRequestsJob consolidateCancelRequestsJob = new ConsolidateCancelRequestsJob(Pending,
                                                                                                         Lookup,
                                                                                                         m_ProgressDataStream.Progress);
            dependsOn = consolidateCancelRequestsJob.Schedule(dependsOn);

            m_ProgressDataStream.AccessController.ReleaseAsync(dependsOn);
            AccessController.ReleaseAsync(dependsOn);
            return dependsOn;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ConsolidateCancelRequestsJob : IJob
        {
            [ReadOnly] private UnsafeTypedStream<EntityProxyInstanceID> m_Pending;
            private UnsafeParallelHashMap<EntityProxyInstanceID, byte> m_Lookup;
            private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_ProgressLookup;

            public ConsolidateCancelRequestsJob(UnsafeTypedStream<EntityProxyInstanceID> pending,
                                                UnsafeParallelHashMap<EntityProxyInstanceID, byte> lookup,
                                                UnsafeParallelHashMap<EntityProxyInstanceID, bool> progressLookup)
            {
                m_Pending = pending;
                m_Lookup = lookup;
                m_ProgressLookup = progressLookup;
            }

            public void Execute()
            {
                m_Lookup.Clear();
                foreach (EntityProxyInstanceID proxyInstanceID in m_Pending)
                {
                    Debug_EnsureNoDuplicates(proxyInstanceID);
                    m_Lookup.TryAdd(proxyInstanceID, 1);
                    //We have something that wants to cancel and we assume that it will cancel immediately. 
                    //A CancelJob will hold it open to allow for multi frame cancellation
                    m_ProgressLookup.TryAdd(proxyInstanceID, false);
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
