using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelCompleteDataStream : AbstractEntityInstanceIDDataStream
    {
        internal DeferredNativeArray<EntityProxyInstanceID> Live { get; }

        internal CancelCompleteDataStream()
        {
            Live = new DeferredNativeArray<EntityProxyInstanceID>(Allocator.Persistent,
                                                                  Allocator.TempJob);
        }

        protected override void DisposeDataStream()
        {
            Live.Dispose();
            base.DisposeDataStream();
        }

        protected override JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            dependsOn = JobHandle.CombineDependencies(dependsOn,
                                                      AccessController.AcquireAsync(AccessType.ExclusiveWrite));

            ConsolidateCancelCompleteJob consolidateCancelCompleteJob = new ConsolidateCancelCompleteJob(Pending, Live);
            dependsOn = consolidateCancelCompleteJob.Schedule(dependsOn);

            AccessController.ReleaseAsync(dependsOn);

            return dependsOn;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ConsolidateCancelCompleteJob : IJob
        {
            [ReadOnly] private UnsafeTypedStream<EntityProxyInstanceID> m_Pending;
            [WriteOnly] private DeferredNativeArray<EntityProxyInstanceID> m_Iteration;

            public ConsolidateCancelCompleteJob(UnsafeTypedStream<EntityProxyInstanceID> pending,
                                                DeferredNativeArray<EntityProxyInstanceID> iteration)
            {
                m_Pending = pending;
                m_Iteration = iteration;
            }

            public void Execute()
            {
                m_Iteration.Clear();

                NativeArray<EntityProxyInstanceID> iterationArray = m_Iteration.DeferredCreate(m_Pending.Count());
                m_Pending.CopyTo(ref iterationArray);
                m_Pending.Clear();
            }
        }
    }
}
