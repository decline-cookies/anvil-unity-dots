using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
#if DEBUG
using Unity.Profiling;
#endif

#if ANVIL_DEBUG_LOGGING_EXPENSIVE
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

            ConsolidateCancelRequestsJob consolidateCancelRequestsJob = new ConsolidateCancelRequestsJob(Pending,
                                                                                                         Lookup,
                                                                                                         progressLookup
#if DEBUG
                                                                                                        ,
                                                                                                         Debug_ProfilingInfo.ProfilingDetails
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                                                                                                    ,
                                                                                                         Debug_DebugString
#endif
                                                                                                        );

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


#if DEBUG
            private DataStreamProfilingDetails m_ProfilingDetails;
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
            private readonly FixedString128Bytes m_DebugString;
#endif


            public ConsolidateCancelRequestsJob(UnsafeTypedStream<EntityProxyInstanceID> pending,
                                                UnsafeParallelHashMap<EntityProxyInstanceID, bool> lookup,
                                                UnsafeParallelHashMap<EntityProxyInstanceID, bool> progressLookup
#if DEBUG
                                               ,
                                                DataStreamProfilingDetails profilingDetails
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                                           ,
                                                FixedString128Bytes debugString
#endif
            )
            {
                m_Pending = pending;
                m_Lookup = lookup;
                m_ProgressLookup = progressLookup;
#if DEBUG
                m_ProfilingDetails = profilingDetails;
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                m_DebugString = debugString;
#endif
            }

            public void Execute()
            {
#if DEBUG
                m_ProfilingDetails.ProfilerMarker.Begin();
                int lookupCount = 0;
#endif

                m_Lookup.Clear();
                foreach (EntityProxyInstanceID proxyInstanceID in m_Pending)
                {
                    Debug_EnsureNoDuplicates(proxyInstanceID);
                    m_Lookup.TryAdd(proxyInstanceID, true);
                    //We have something that wants to cancel, so we assume that it will get processed this frame.
                    //If nothing processes it, it will auto-complete the next frame. 
                    m_ProgressLookup.TryAdd(proxyInstanceID, true);
#if DEBUG
                    lookupCount++;
#endif
                }

                m_Pending.Clear();

#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                if (!m_Lookup.IsEmpty)
                {
                    Debug.Log($"{m_DebugString} - Count {m_Lookup.Count()}");
                }
#endif
#if DEBUG
                m_ProfilingDetails.PendingCapacity = m_Pending.Capacity();
                m_ProfilingDetails.LiveInstances = lookupCount;
                m_ProfilingDetails.LiveCapacity = m_Lookup.Capacity;
                m_ProfilingDetails.ProfilerMarker.End();
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
