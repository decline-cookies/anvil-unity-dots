using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public class IgnoreCancelDataStream<TInstance> : AbstractDataStream<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        //TODO: Gross but we'll just ignore the taskDriverCancelRequests for now
        internal IgnoreCancelDataStream(CancelRequestDataStream taskDriverCancelRequests, AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem) : base(false, taskDriver, taskSystem)
        {
        }

        internal override AbstractDataStream GetCancelPendingDataStream()
        {
            throw new NotImplementedException("never should be called!");
        }

        protected override JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            dependsOn = JobHandle.CombineDependencies(dependsOn,
                                                      AccessController.AcquireAsync(AccessType.ExclusiveWrite));

            ConsolidateIgnoredCancelJob consolidateIgnoredCancelJob = new ConsolidateIgnoredCancelJob(Pending,
                                                                                                      Live,
                                                                                                      Debug_DebugString,
                                                                                                      Debug_ProfilerMarker);
            dependsOn = consolidateIgnoredCancelJob.Schedule(dependsOn);

            AccessController.ReleaseAsync(dependsOn);
            return dependsOn;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ConsolidateIgnoredCancelJob : IJob
        {
            [ReadOnly] private UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> m_Pending;
            [WriteOnly] private DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> m_Iteration;

            private readonly FixedString128Bytes m_DebugString;
            private readonly ProfilerMarker m_ProfilerMarker;

            public ConsolidateIgnoredCancelJob(UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> pending,
                                               DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> iteration,
                                               FixedString128Bytes debugString,
                                               ProfilerMarker profilerMarker) : this()
            {
                m_Pending = pending;
                m_Iteration = iteration;
                m_DebugString = debugString;
                m_ProfilerMarker = profilerMarker;
            }

            public void Execute()
            {
                m_ProfilerMarker.Begin();
                m_Iteration.Clear();

                int pendingCount = m_Pending.Count();
                NativeArray<EntityProxyInstanceWrapper<TInstance>> iterationArray = m_Iteration.DeferredCreate(pendingCount);
                m_Pending.CopyTo(ref iterationArray);
                m_Pending.Clear();

                if (iterationArray.Length > 0)
                {
                    Debug.Log($"{m_DebugString} - Count {iterationArray.Length}");
                }

                m_ProfilerMarker.End();
            }
        }
    }
}
