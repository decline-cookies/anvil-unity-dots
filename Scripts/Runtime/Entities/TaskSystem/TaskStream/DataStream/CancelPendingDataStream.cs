using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Profiling;
using Unity.Profiling.LowLevel;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelPendingDataStream<TInstance> : AbstractDataStream<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        //TODO: Hide this stuff when collections checks are disabled
        private static readonly ProfilerMarker TYPED_MARKER = new ProfilerMarker(ProfilerCategory.Scripts, typeof(ConsolidateCancelledJob).GetReadableName(), MarkerFlags.Script);

        internal CancelPendingDataStream() : base(false)
        {
        }

        internal override AbstractDataStream GetCancelPendingDataStream()
        {
            throw new System.NotImplementedException("never should be called!");
        }

        protected override JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            dependsOn = JobHandle.CombineDependencies(dependsOn,
                                                      AccessController.AcquireAsync(AccessType.ExclusiveWrite));

            ConsolidateCancelledJob consolidateCancelledJob = new ConsolidateCancelledJob(Pending,
                                                                                          Live,
                                                                                          TYPED_MARKER);
            dependsOn = consolidateCancelledJob.Schedule(dependsOn);

            AccessController.ReleaseAsync(dependsOn);
            return dependsOn;
        }

        internal DataStreamCancellationUpdater<TInstance> CreateDataStreamCancellationUpdater(DataStreamTargetResolver dataStreamTargetResolver,
                                                                                              UnsafeParallelHashMap<EntityProxyInstanceID, bool> cancelProgressLookup)
        {
            return new DataStreamCancellationUpdater<TInstance>(Pending.AsWriter(),
                                                                Live.AsDeferredJobArray(),
                                                                dataStreamTargetResolver,
                                                                cancelProgressLookup);
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ConsolidateCancelledJob : IJob
        {
            [ReadOnly] private UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> m_Pending;
            [WriteOnly] private DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> m_Iteration;
            private ProfilerMarker m_Marker;

            public ConsolidateCancelledJob(UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> pending,
                                           DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> iteration,
                                           ProfilerMarker marker) : this()
            {
                m_Pending = pending;
                m_Iteration = iteration;
                m_Marker = marker;
            }

            public void Execute()
            {
                m_Marker.Begin();
                m_Iteration.Clear();

                int pendingCount = m_Pending.Count();
                NativeArray<EntityProxyInstanceWrapper<TInstance>> iterationArray = m_Iteration.DeferredCreate(pendingCount);
                m_Pending.CopyTo(ref iterationArray);
                m_Pending.Clear();
                m_Marker.End();
            }
        }
    }
}
