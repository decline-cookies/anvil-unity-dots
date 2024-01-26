using Anvil.CSharp.Mathematics;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Entities;
using Anvil.Unity.DOTS.TestCase.SharedWrite;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;

#if ANVIL_TEST_CASE_SHARED_WRITE
[assembly: RegisterGenericJobType(typeof(SharedReaderSystemPart<SWTCBufferX>.SharedReadJob))]
[assembly: RegisterGenericJobType(typeof(SharedReaderSystemPart<SWTCBufferY>.SharedReadJob))]
#endif

namespace Anvil.Unity.DOTS.TestCase.SharedWrite
{
#if ANVIL_TEST_CASE_SHARED_WRITE
    [BurstCompile]
    public struct SharedReaderSystemPart<T>
        where T : unmanaged, IComponentData, ISWTCBuffer
    {
        private readonly int m_Index;
        private readonly int m_Value;
        private readonly ProfilerMarker m_ProfilerMarker;
        private EntityQuery m_SingletonQuery;

        public SharedReaderSystemPart(
            ref SystemState state,
            int index,
            int value,
            ProfilerMarker profilerMarker)
        {
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

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.GetSystemDependency(out JobHandle dependsOn);
            T data = m_SingletonQuery.GetSingleton<T>();

            SharedReadJob job = new SharedReadJob(
                                                  ref data,
                                                  m_Index,
                                                  m_Value,
                                                  m_ProfilerMarker);
            dependsOn = job.ScheduleByRef(dependsOn);

            state.SetSystemDependency(dependsOn);
        }

        [BurstCompile]
        internal struct SharedReadJob : IJob
        {
            private readonly int m_Index;
            private readonly int m_Value;
            private readonly ProfilerMarker m_ProfilerMarker;

            [ReadOnly] private T m_Data;

            public SharedReadJob(ref T data, int index, int value, ProfilerMarker profilerMarker)
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
                ref readonly int element = ref m_Data.Buffer.ElementAtReadOnly(m_Index);

                Debug.Assert(element == m_Value);
                m_ProfilerMarker.End();
            }
        }
    }
#endif
}