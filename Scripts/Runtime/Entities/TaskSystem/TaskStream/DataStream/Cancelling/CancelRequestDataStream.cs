using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Profiling;
using Debug = UnityEngine.Debug;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelRequestDataStream : AbstractEntityInstanceIDDataStream
    {
        private static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<EntityProxyInstanceID>();
        
        private readonly CancelData m_CancelData;
        internal UnsafeParallelHashMap<EntityProxyInstanceID, byte> Lookup { get; }
        
        public CancelRequestDataStream(CancelData cancelData, AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem) : base(taskDriver, taskSystem)
        {
            m_CancelData = cancelData;
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
                                                      m_CancelData.AcquireProgressLookup(AccessType.ExclusiveWrite, out UnsafeParallelHashMap<EntityProxyInstanceID, bool> progressLookup));

            ConsolidateCancelRequestsJob consolidateCancelRequestsJob = new ConsolidateCancelRequestsJob(Pending,
                                                                                                         Lookup,
                                                                                                         progressLookup,
                                                                                                         Debug_DebugString,
                                                                                                         Debug_ProfilerMarker);
            dependsOn = consolidateCancelRequestsJob.Schedule(dependsOn);

            m_CancelData.ReleaseProgressLookup(dependsOn);
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
            
            private readonly FixedString128Bytes m_DebugString;
            private readonly ProfilerMarker m_ProfilerMarker;

            public ConsolidateCancelRequestsJob(UnsafeTypedStream<EntityProxyInstanceID> pending,
                                                UnsafeParallelHashMap<EntityProxyInstanceID, byte> lookup,
                                                UnsafeParallelHashMap<EntityProxyInstanceID, bool> progressLookup,
                                                FixedString128Bytes debugString,
                                                ProfilerMarker profilerMarker)
            {
                m_Pending = pending;
                m_Lookup = lookup;
                m_ProgressLookup = progressLookup;
                m_DebugString = debugString;
                m_ProfilerMarker = profilerMarker;
            }

            public void Execute()
            {
                m_ProfilerMarker.Begin();
                
                m_Lookup.Clear();
                foreach (EntityProxyInstanceID proxyInstanceID in m_Pending)
                {
                    Debug_EnsureNoDuplicates(proxyInstanceID);
                    m_Lookup.TryAdd(proxyInstanceID, 1);
                    //We have something that wants to cancel, so we assume that it will get processed this frame.
                    //If nothing processes it, it will auto-complete the next frame. 
                    m_ProgressLookup.TryAdd(proxyInstanceID, true);
                }

                if (!m_Lookup.IsEmpty)
                {
                    Debug.Log($"{m_DebugString} - Count {m_Lookup.Count()}");
                }

                m_Pending.Clear();
                
                m_ProfilerMarker.End();
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
