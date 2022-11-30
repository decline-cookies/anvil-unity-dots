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
        private readonly CancelCompleteDataStream m_CancelCompleteDataStream;

        private bool HasCancellableData
        {
            get => OwningTaskDriver?.HasCancellableData ?? OwningTaskSystem.HasCancellableData;
        }

        public CancelRequestDataStream(AccessControlledValue<UnsafeParallelHashMap<EntityProxyInstanceID, bool>> cancelProgressLookup,
                                       CancelCompleteDataStream cancelCompleteDataStream,
                                       AbstractTaskDriverWork owningTaskDriverWork) : base(owningTaskDriverWork)
        {
            m_CancelProgressLookup = cancelProgressLookup;
            m_CancelCompleteDataStream = cancelCompleteDataStream;
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
            return HasCancellableData
                ? ConsolidateWithCancellableData(dependsOn)
                : ConsolidateWithoutCancellableData(dependsOn);
        }

        private JobHandle ConsolidateWithCancellableData(JobHandle dependsOn)
        {
            dependsOn = JobHandle.CombineDependencies(dependsOn,
                                                      AccessController.AcquireAsync(AccessType.ExclusiveWrite),
                                                      m_CancelProgressLookup.AcquireAsync(AccessType.ExclusiveWrite, out UnsafeParallelHashMap<EntityProxyInstanceID, bool> progressLookup));

            ConsolidateCancelRequestsWithCancellableDataJob job = new ConsolidateCancelRequestsWithCancellableDataJob(Pending,
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

            dependsOn = job.Schedule(dependsOn);

            m_CancelProgressLookup.ReleaseAsync(dependsOn);
            AccessController.ReleaseAsync(dependsOn);
            return dependsOn;
        }

        private JobHandle ConsolidateWithoutCancellableData(JobHandle dependsOn)
        {
            dependsOn = JobHandle.CombineDependencies(dependsOn,
                                                      AccessController.AcquireAsync(AccessType.ExclusiveWrite),
                                                      m_CancelCompleteDataStream.AccessController.AcquireAsync(AccessType.SharedWrite));

            ConsolidateCancelRequestsWithoutCancellableDataJob job = new ConsolidateCancelRequestsWithoutCancellableDataJob(Pending,
                                                                                                                            Lookup,
                                                                                                                            m_CancelCompleteDataStream.Pending.AsWriter()
#if DEBUG
                                                                                                                           ,
                                                                                                                            Debug_ProfilingInfo.ProfilingDetails
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                                                                                                                           ,
                                                                                                                            Debug_DebugString
#endif
                                                                                                                           );

            dependsOn = job.Schedule(dependsOn);

            m_CancelCompleteDataStream.AccessController.ReleaseAsync(dependsOn);
            AccessController.ReleaseAsync(dependsOn);
            return dependsOn;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ConsolidateCancelRequestsWithCancellableDataJob : IJob
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


            public ConsolidateCancelRequestsWithCancellableDataJob(UnsafeTypedStream<EntityProxyInstanceID> pending,
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
                    Debug_EnsureNoDuplicates(proxyInstanceID, ref m_Lookup);
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
        }

        [BurstCompile]
        private struct ConsolidateCancelRequestsWithoutCancellableDataJob : IJob
        {
            private const int UNSET_THREAD_INDEX = -1;

            [NativeSetThreadIndex] [ReadOnly] private readonly int m_NativeThreadIndex;

            [ReadOnly] private UnsafeTypedStream<EntityProxyInstanceID> m_Pending;
            private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_Lookup;
            [ReadOnly] private readonly UnsafeTypedStream<EntityProxyInstanceID>.Writer m_CompleteWriter;

            private UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter m_CompleteLaneWriter;


#if DEBUG
            private DataStreamProfilingDetails m_ProfilingDetails;
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
            private readonly FixedString128Bytes m_DebugString;
#endif


            public ConsolidateCancelRequestsWithoutCancellableDataJob(UnsafeTypedStream<EntityProxyInstanceID> pending,
                                                                      UnsafeParallelHashMap<EntityProxyInstanceID, bool> lookup,
                                                                      UnsafeTypedStream<EntityProxyInstanceID>.Writer completeWriter
#if DEBUG
                                                                     ,
                                                                      DataStreamProfilingDetails profilingDetails
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                                                                     ,
                                                                      FixedString128Bytes debugString
#endif
            ) : this()
            {
                m_Pending = pending;
                m_Lookup = lookup;
                m_CompleteWriter = completeWriter;
#if DEBUG
                m_ProfilingDetails = profilingDetails;
#endif
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                m_DebugString = debugString;
#endif

                m_NativeThreadIndex = UNSET_THREAD_INDEX;
            }

            public void Execute()
            {
#if DEBUG
                m_ProfilingDetails.ProfilerMarker.Begin();
                int lookupCount = 0;
#endif
                int laneIndex = ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex);
                m_CompleteLaneWriter = m_CompleteWriter.AsLaneWriter(laneIndex);

                m_Lookup.Clear();
                foreach (EntityProxyInstanceID proxyInstanceID in m_Pending)
                {
                    Debug_EnsureNoDuplicates(proxyInstanceID, ref m_Lookup);
                    m_Lookup.TryAdd(proxyInstanceID, true);
#if ANVIL_DEBUG_LOGGING_EXPENSIVE
                    Debug.Log($"No cancel flow needed for {proxyInstanceID.ToFixedString()} on {m_DebugString} - Completing Early");
#endif
                    m_CompleteLaneWriter.Write(proxyInstanceID);
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
        }


        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ANVIL_DEBUG_SAFETY_EXPENSIVE")]
        private static void Debug_EnsureNoDuplicates(EntityProxyInstanceID id, ref UnsafeParallelHashMap<EntityProxyInstanceID, bool> lookup)
        {
            if (lookup.ContainsKey(id))
            {
                throw new InvalidOperationException($"Trying to add id of {id} but the same id already exists in the lookup! This should never happen! Investigate.");
            }
        }
    }
}
