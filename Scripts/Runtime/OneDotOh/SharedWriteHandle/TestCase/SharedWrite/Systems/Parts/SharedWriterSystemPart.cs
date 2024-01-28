using Anvil.CSharp.Mathematics;
using Anvil.Unity.DOTS.Core;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Entities;
using Anvil.Unity.DOTS.TestCase.SharedWrite;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;

#if ANVIL_TEST_CASE_SHARED_WRITE
[assembly: RegisterGenericJobType(typeof(SharedWriterSystemPart<SWTCBufferX>.SharedWriteJob))]
[assembly: RegisterGenericJobType(typeof(SharedWriterSystemPart<SWTCBufferY>.SharedWriteJob))]
#endif

namespace Anvil.Unity.DOTS.TestCase.SharedWrite
{
#if ANVIL_TEST_CASE_SHARED_WRITE
    [BurstCompile]
    public struct SharedWriterSystemPart<T>
        where T : unmanaged, IComponentData, ISWTCBuffer
    {
        private readonly SharedWriteTrigger m_ExpectedSharedWriteTrigger;
        private readonly int m_Index;
        private readonly int m_Value;
        private readonly ProfilerMarker m_ProfilerMarker;
        private EntityQuery m_SingletonQuery;

        public SharedWriterSystemPart(
            ref SystemState state,
            SharedWriteTrigger expectedSharedWriteTrigger,
            int index,
            int value,
            ProfilerMarker profilerMarker)
        {
            m_ExpectedSharedWriteTrigger = expectedSharedWriteTrigger;
            m_Index = index;
            m_Value = value;
            m_ProfilerMarker = profilerMarker;

            EntityQueryBuilder queryBuilder = new EntityQueryBuilder(AllocatorManager.Temp)
                                             .WithAll<T>()
                                             .WithOptions(EntityQueryOptions.Default | EntityQueryOptions.IncludeSystems);
            m_SingletonQuery = queryBuilder.Build(ref state);
            queryBuilder.Dispose();

            state.RequireForUpdate<T>();
        }

        public void OnUpdate(ref SystemState state)
        {
            // state.GetSystemDependency(out JobHandle dependsOn);

            state.AcquireSharedWriteHandle<T>(out JobHandle sharedWriteHandle, out SharedWriteTrigger sharedWriteTrigger);
            Debug.Assert(sharedWriteTrigger == m_ExpectedSharedWriteTrigger);

            T data = m_SingletonQuery.GetSingleton<T>();

            SharedWriteJob job = new SharedWriteJob(
                                                    ref data,
                                                    m_Index,
                                                    m_Value,
                                                    m_ProfilerMarker);

            // If we're new, then we need to account for all other dependencies coming in here.
            // if (sharedWriteTrigger == SharedWriteTrigger.New)
            // {
            //     dependsOn = JobHandle.CombineDependencies(dependsOn, sharedWriteHandle);
            // }
            // If we're another Shared Write job in the chain, we don't want to use the built in dependency because
            // then we would be scheduled after the first Shared Write job. Instead, we want to schedule back when the
            // first job happened.
            // TODO: What happens if the system dependency contained data that we need too? Are we potentially having a conflict?
            // Example Case -
            // JobA - Takes in a bunch of entities to read some data from them and write to other data but also SharedWrite to an event buffer.
            // JobB - Takes in different data to read and write but also SharedWrites to the same event buffer.
            // If the data for both JobA and JobB doesn't conflict in any way, then we should be fine to just schedule based on the SharedWriteHandle.
            // If the data for both JobA and JobB does conflict, ex, they both need to write to the same place exclusively. Then the SharedWriteHandle will cause a conflict.
            // Need to create two test cases to test for this assertion.
            // else
            // {
            //     dependsOn = sharedWriteHandle;
            // }

            // TODO: Cleanup - This will have the system dependency baked in minus the shared write component.
            // TODO: If we have multiple shared writes for the same job, we need a way to account for that.
            JobHandle dependsOn = sharedWriteHandle;
            dependsOn = job.ScheduleByRef(dependsOn);

            // TODO: We want to schedule the job based on the SharedWrite handle combined with the modified system dependency.
            // However, we then want to move the Write and Read handle forward by the original system dependency combined with the job we just scheduled out of band
            // so that everything still jives.
            // dependsOn = JobHandle.CombineDependencies(dependsOn, state.Dependency);
            state.ReleaseSharedWriteHandle<T>(ref dependsOn);

            state.SetSystemDependency(dependsOn);
        }

        [BurstCompile]
        internal struct SharedWriteJob : IJob
        {
            [NativeDisableContainerSafetyRestriction]
            private T m_Data;

            private readonly int m_Index;
            private readonly int m_Value;
            private readonly ProfilerMarker m_ProfilerMarker;

            public SharedWriteJob(
                ref T data,
                int index,
                int value,
                ProfilerMarker profilerMarker)
            {
                m_Data = data;
                m_Index = index;
                m_Value = value;
                m_ProfilerMarker = profilerMarker;
            }

            public void Execute()
            {
                m_ProfilerMarker.Begin();
                MathUtil.FindPrimeNumber(SWTCConstants.NTH_PRIME_VALUE_TO_FIND);
                ref int element = ref m_Data.Buffer.ElementAt(m_Index);
                element = m_Value;
                m_ProfilerMarker.End();
            }
        }
    }
#endif
}