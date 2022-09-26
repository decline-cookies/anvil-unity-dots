using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class CancelRequestsDataStream : AbstractProxyDataStream
    {
        public static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<ProxyInstanceID>();

        private UnsafeTypedStream<ProxyInstanceID> m_Pending;
        private UnsafeParallelHashMap<ProxyInstanceID, byte> m_Lookup;
        public CancelRequestsDataStream()
        {
            m_Pending = new UnsafeTypedStream<ProxyInstanceID>(Allocator.Persistent);
            m_Lookup = new UnsafeParallelHashMap<ProxyInstanceID, byte>(MAX_ELEMENTS_PER_CHUNK, Allocator.Persistent);
        }

        protected override void DisposeSelf()
        {
            AccessController.Acquire(AccessType.Disposal);
            m_Pending.Dispose();
            m_Lookup.Dispose();
            
            
            base.DisposeSelf();
        }
        
        internal sealed override unsafe void* GetWriterPointer()
        {
            throw new NotSupportedException($"Trying to access the writer pointer for {this} but that is not supported!");
        }

        //*************************************************************************************************************
        // SERIALIZATION
        //*************************************************************************************************************

        //TODO: Add support for Serialization. Hopefully from the outside or via extension methods instead of functions
        //here but keeping the TODO for future reminder.
        
        //*************************************************************************************************************
        // JOB STRUCTS
        //*************************************************************************************************************
        
        internal CancelRequestsReader CreateRequestCancelReader()
        {
            return new CancelRequestsReader(m_Lookup);
        }
        
        //*************************************************************************************************************
        // CONSOLIDATION
        //*************************************************************************************************************
        protected sealed override JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            JobHandle exclusiveWriteHandle = AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            ConsolidateJob consolidateJob = new ConsolidateJob(m_Pending,
                                                               m_Lookup);
            JobHandle consolidateHandle = consolidateJob.Schedule(JobHandle.CombineDependencies(dependsOn, exclusiveWriteHandle));

            AccessController.ReleaseAsync(consolidateHandle);

            return consolidateHandle;
        }
        
        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ConsolidateJob : IJob
        {
            private UnsafeTypedStream<ProxyInstanceID> m_Pending;
            private UnsafeParallelHashMap<ProxyInstanceID, byte> m_Lookup;

            public ConsolidateJob(UnsafeTypedStream<ProxyInstanceID> pending,
                                  UnsafeParallelHashMap<ProxyInstanceID, byte> lookup)
            {
                m_Pending = pending;
                m_Lookup = lookup;
            }

            public void Execute()
            {
                m_Lookup.Clear();

                foreach (ProxyInstanceID proxyInstanceID in m_Pending)
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
            private void Debug_EnsureNoDuplicates(ProxyInstanceID id)
            {
                if (m_Lookup.ContainsKey(id))
                {
                    throw new InvalidOperationException($"Trying to add id of {id} but the same id already exists in the lookup! This should never happen! Investigate.");
                }
            }
        }
    }
}
