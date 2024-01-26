using Anvil.CSharp.Mathematics;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Entities;
using Anvil.Unity.DOTS.TestCase.SharedWrite;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Profiling;

#if ANVIL_TEST_CASE_SHARED_WRITE
[assembly: RegisterGenericJobType(typeof(ExclusiveWriterSystemPart<SWTCBufferX>.ExclusiveWriteJob))]
[assembly: RegisterGenericJobType(typeof(ExclusiveWriterSystemPart<SWTCBufferY>.ExclusiveWriteJob))]
#endif

namespace Anvil.Unity.DOTS.TestCase.SharedWrite
{
#if ANVIL_TEST_CASE_SHARED_WRITE
    [BurstCompile]
    public struct ExclusiveWriterSystemPart<T>
        where T : unmanaged, IComponentData, ISWTCBuffer
    {
        private readonly int m_Index;
        private readonly int m_Value;
        private readonly ProfilerMarker m_ProfilerMarker;
        private EntityQuery m_SingletonQuery;


        public ExclusiveWriterSystemPart(
            ref SystemState state,
            int index,
            int value,
            ProfilerMarker profilerMarker)
        {
            m_Index = index;
            m_Value = value;
            m_ProfilerMarker = profilerMarker;

            EntityQueryBuilder queryBuilder = new EntityQueryBuilder(AllocatorManager.Temp)
                                             .WithAllRW<T>()
                                             .WithOptions(EntityQueryOptions.Default | EntityQueryOptions.IncludeSystems);
            m_SingletonQuery = queryBuilder.Build(ref state);
            queryBuilder.Dispose();

            state.RequireForUpdate<T>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.GetSystemDependency(out JobHandle dependsOn);
            RefRW<T> data = m_SingletonQuery.GetSingletonRW<T>();

            ExclusiveWriteJob job = new ExclusiveWriteJob(
                                                          ref data.ValueRW,
                                                          m_Index,
                                                          m_Value,
                                                          m_ProfilerMarker);
            dependsOn = job.ScheduleByRef(dependsOn);

            state.SetSystemDependency(dependsOn);
        }

        [BurstCompile]
        internal struct ExclusiveWriteJob : IJob
        {
            private readonly int m_Index;
            private readonly int m_Value;
            private readonly ProfilerMarker m_ProfilerMarker;

            private T m_Data;

            public ExclusiveWriteJob(ref T data, int index, int value, ProfilerMarker profilerMarker)
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