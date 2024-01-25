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
        private EntityQuery m_SingletonQuery;

        public SharedWriterSystemPart(
            ref SystemState state,
            SharedWriteTrigger expectedSharedWriteTrigger,
            int index,
            int value)
        {
            m_ExpectedSharedWriteTrigger = expectedSharedWriteTrigger;
            m_Index = index;
            m_Value = value;

            EntityQueryBuilder queryBuilder = new EntityQueryBuilder(AllocatorManager.Temp)
                                             .WithAll<T>()
                                             .WithOptions(EntityQueryOptions.Default | EntityQueryOptions.IncludeSystems);
            m_SingletonQuery = queryBuilder.Build(ref state);
            queryBuilder.Dispose();

            state.RequireForUpdate<T>();
        }

        public void OnUpdate(ref SystemState state)
        {
            state.GetSystemDependency(out JobHandle dependsOn);

            state.AcquireSharedWriteHandle<T>(out JobHandle sharedWriteHandle, out SharedWriteTrigger sharedWriteTrigger);
            Debug.Assert(sharedWriteTrigger == m_ExpectedSharedWriteTrigger);

            T data = m_SingletonQuery.GetSingleton<T>();

            SharedWriteJob job = new SharedWriteJob(
                                                    ref data,
                                                    m_Index,
                                                    m_Value);

            // If we're new, then we need to account for all other dependencies coming in here.
            if (sharedWriteTrigger == SharedWriteTrigger.New)
            {
                dependsOn = JobHandle.CombineDependencies(dependsOn, sharedWriteHandle);
            }
            // If we're another Shared Write job in the chain, we don't want to use the built in dependency because
            // then we would be scheduled after the first Shared Write job. Instead, we want to schedule back when the
            // first job happened.
            else
            {
                dependsOn = sharedWriteHandle;
            }

            dependsOn = job.ScheduleByRef(dependsOn);

            state.ReleaseSharedWriteHandle<T>(dependsOn);

            state.SetSystemDependency(JobHandle.CombineDependencies(dependsOn, state.Dependency));
        }

        [BurstCompile]
        internal struct SharedWriteJob : IJob
        {
            [NativeDisableContainerSafetyRestriction]
            private T m_Data;

            private readonly int m_Index;
            private readonly int m_Value;

            public SharedWriteJob(
                ref T data,
                int index,
                int value)
            {
                m_Data = data;
                m_Index = index;
                m_Value = value;
            }

            public void Execute()
            {
                MathUtil.FindPrimeNumber(SWTCConstants.NTH_PRIME_VALUE_TO_FIND);
                ref int element = ref m_Data.Buffer.ElementAt(m_Index);
                element = m_Value;
            }
        }
    }
#endif
}