using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
#if ANVIL_DEBUG_SAFETY_EXPENSIVE
using Unity.Profiling;
using Debug = UnityEngine.Debug;
#endif

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelRequestDataStream : AbstractLookupDataStream<EntityProxyInstanceID>
    {
        private readonly AccessControlledValue<UnsafeParallelHashMap<EntityProxyInstanceID, bool>> m_CancelProgressLookup;

        public CancelRequestDataStream(AccessControlledValue<UnsafeParallelHashMap<EntityProxyInstanceID, bool>> cancelProgressLookup, 
                                       AbstractTaskDriver taskDriver, 
                                       AbstractTaskSystem taskSystem) : base(taskDriver, taskSystem)
        {
            m_CancelProgressLookup = cancelProgressLookup;
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
                                                      m_CancelProgressLookup.AcquireAsync(AccessType.ExclusiveWrite, out UnsafeParallelHashMap<EntityProxyInstanceID, bool> progressLookup));
#if ANVIL_DEBUG_SAFETY_EXPENSIVE
            ConsolidateCancelRequestsJob consolidateCancelRequestsJob = new ConsolidateCancelRequestsJob(Pending,
                                                                                                         Lookup,
                                                                                                         progressLookup,
                                                                                                         Debug_DebugString,
                                                                                                         Debug_ProfilerMarker);
#else
            ConsolidateCancelRequestsJob consolidateCancelRequestsJob = new ConsolidateCancelRequestsJob(Pending,
                                                                                                         Lookup,
                                                                                                         progressLookup);
#endif

            dependsOn = consolidateCancelRequestsJob.Schedule(dependsOn);

            m_CancelProgressLookup.ReleaseAsync(dependsOn);
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
            private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_Lookup;
            private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_ProgressLookup;

#if ANVIL_DEBUG_SAFETY_EXPENSIVE
            private readonly FixedString128Bytes m_DebugString;
            private readonly ProfilerMarker m_ProfilerMarker;
#endif

#if ANVIL_DEBUG_SAFETY_EXPENSIVE
            public ConsolidateCancelRequestsJob(UnsafeTypedStream<EntityProxyInstanceID> pending,
                                                UnsafeParallelHashMap<EntityProxyInstanceID, bool> lookup,
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
#else
            public ConsolidateCancelRequestsJob(UnsafeTypedStream<EntityProxyInstanceID> pending,
                                                UnsafeParallelHashMap<EntityProxyInstanceID, bool> lookup,
                                                UnsafeParallelHashMap<EntityProxyInstanceID, bool> progressLookup)
            {
                m_Pending = pending;
                m_Lookup = lookup;
                m_ProgressLookup = progressLookup;
            }
#endif

            public void Execute()
            {
#if ANVIL_DEBUG_SAFETY_EXPENSIVE
                m_ProfilerMarker.Begin();
#endif

                m_Lookup.Clear();
                foreach (EntityProxyInstanceID proxyInstanceID in m_Pending)
                {
                    Debug_EnsureNoDuplicates(proxyInstanceID);
                    m_Lookup.TryAdd(proxyInstanceID, true);
                    //We have something that wants to cancel, so we assume that it will get processed this frame.
                    //If nothing processes it, it will auto-complete the next frame. 
                    m_ProgressLookup.TryAdd(proxyInstanceID, true);
                }

                m_Pending.Clear();

#if ANVIL_DEBUG_SAFETY_EXPENSIVE
                if (!m_Lookup.IsEmpty)
                {
                    Debug.Log($"{m_DebugString} - Count {m_Lookup.Count()}");
                }

                m_ProfilerMarker.End();
#endif
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
