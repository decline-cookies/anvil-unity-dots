using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public class CancelProgressDataStream : AbstractConsolidatableDataStream
    {

        private readonly CancelCompleteDataStream m_CancelCompleteDataStream;
        internal UnsafeParallelHashMap<EntityProxyInstanceID, bool> Progress { get; }

        internal CancelProgressDataStream(CancelCompleteDataStream cancelCompleteDataStream)
        {
            m_CancelCompleteDataStream = cancelCompleteDataStream;
            Progress = new UnsafeParallelHashMap<EntityProxyInstanceID, bool>(ChunkUtil.MaxElementsPerChunk<EntityProxyInstanceID>(),
                                                                              Allocator.Persistent);
        }

        protected override void DisposeDataStream()
        {
            Progress.Dispose();
            base.DisposeDataStream();
        } 

        protected override JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            //TODO: How do we get the completion to run up the chain?
            dependsOn = JobHandle.CombineDependencies(dependsOn,
                                                      AccessController.AcquireAsync(AccessType.ExclusiveWrite),
                                                      m_CancelCompleteDataStream.AccessController.AcquireAsync(AccessType.SharedWrite));

            ConsolidateCancelProgressJob consolidateCancelProgressJob = new ConsolidateCancelProgressJob(Progress,
                                                                                                         m_CancelCompleteDataStream.Pending.AsWriter());

            dependsOn = consolidateCancelProgressJob.Schedule(dependsOn);
            
            AccessController.ReleaseAsync(dependsOn);
            return dependsOn;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ConsolidateCancelProgressJob : IJob
        {
            [NativeSetThreadIndex][ReadOnly] private readonly int m_NativeThreadIndex;
            private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_ProgressLookup;
            private readonly UnsafeTypedStream<EntityProxyInstanceID>.Writer m_CancelCompleteWriter;

            public ConsolidateCancelProgressJob(UnsafeParallelHashMap<EntityProxyInstanceID, bool> progressLookup, 
                                                UnsafeTypedStream<EntityProxyInstanceID>.Writer cancelCompleteWriter) : this()
            {
                m_ProgressLookup = progressLookup;
                m_CancelCompleteWriter = cancelCompleteWriter;
            }

            public void Execute()
            {
                int laneIndex = ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex);
                UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter cancelCompleteLaneWriter = m_CancelCompleteWriter.AsLaneWriter(laneIndex);

                NativeArray<EntityProxyInstanceID> keys = m_ProgressLookup.GetKeyArray(Allocator.Temp);
                for (int i = 0; i < keys.Length; ++i)
                {
                    EntityProxyInstanceID id = keys[i];
                    bool isStillBeingProcessed = m_ProgressLookup[id];
                    if (isStillBeingProcessed)
                    {
                        //Reset the progress, a Cancel Job that needs to continue this will flip it back
                        m_ProgressLookup[id] = false;
                    }
                    else
                    {
                        //All possible Cancel Jobs that could have run are done (or never needed to be run)
                        m_ProgressLookup.Remove(id);
                        cancelCompleteLaneWriter.Write(id);
                    }
                }

            }
        }
    }
}
