using Anvil.CSharp.Logging;
using Anvil.CSharp.Reflection;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using Unity.Profiling.LowLevel;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class PendingCancelEntityProxyDataStream<TInstance> : AbstractEntityProxyDataStream
        where TInstance : unmanaged, IEntityProxyInstance
    {
        //TODO: Hide this stuff when collections checks are disabled
        private static readonly ProfilerMarker TYPED_MARKER = new ProfilerMarker(ProfilerCategory.Scripts, typeof(ConsolidateCancelledJob).GetReadableName(), MarkerFlags.Script);
        
        private UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> m_Pending;
        private DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> m_IterationTarget;

        internal UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.Writer PendingWriter
        {
            get => m_Pending.AsWriter();
        }
        
        internal DeferredNativeArrayScheduleInfo ScheduleInfo
        {
            get => m_IterationTarget.ScheduleInfo;
        }

        internal override AbstractEntityProxyDataStream GetPendingCancelDataStream()
        {
            throw new System.NotImplementedException("never call this");
        }

        internal PendingCancelEntityProxyDataStream() : base()
        {
            m_Pending = new UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>(Allocator.Persistent);
            m_IterationTarget = new DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>>(Allocator.Persistent,
                                                                                               Allocator.TempJob);
        }

        protected override void DisposeSelf()
        {
            AccessController.Acquire(AccessType.Disposal);
            m_Pending.Dispose();
            m_IterationTarget.Dispose();
            
            base.DisposeSelf();
        }

        internal sealed override unsafe void* GetWriterPointer()
        {
            throw new System.NotImplementedException("never call this");
        }

        protected override JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            JobHandle exclusiveWriteHandle = AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            ConsolidateCancelledJob consolidateCancelledJob = new ConsolidateCancelledJob(m_Pending,
                                                                                          m_IterationTarget,
                                                                                          TYPED_MARKER);
            JobHandle consolidateHandle = consolidateCancelledJob.Schedule(JobHandle.CombineDependencies(dependsOn, exclusiveWriteHandle));
            
            AccessController.ReleaseAsync(consolidateHandle);
            return consolidateHandle;
        }
        
        internal DataStreamCancellationUpdater<TInstance> CreateDataStreamCancellationUpdater(DataStreamTargetResolver dataStreamTargetResolver)
        {
            return new DataStreamCancellationUpdater<TInstance>(m_Pending.AsWriter(),
                                                                m_IterationTarget.AsDeferredJobArray(),
                                                                dataStreamTargetResolver);
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
