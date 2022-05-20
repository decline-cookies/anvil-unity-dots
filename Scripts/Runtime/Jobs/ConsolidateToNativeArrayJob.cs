using Anvil.Unity.DOTS.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Jobs
{
    [BurstCompile]
    public struct ConsolidateToNativeArrayJob<T> : IJob
        where T : struct
    {
        [ReadOnly] private readonly UnsafeTypedStream<T>.Reader m_Reader;
        private DeferredNativeArray<T> m_DeferredNativeArray;

        public ConsolidateToNativeArrayJob(UnsafeTypedStream<T>.Reader reader,
                                           DeferredNativeArray<T> deferredNativeArray)
        {
            m_Reader = reader;
            m_DeferredNativeArray = deferredNativeArray;
        }

        public void Execute()
        {
            int newLength = m_Reader.Count();

            if (newLength == 0)
            {
                return;
            }

            NativeArray<T> array = m_DeferredNativeArray.DeferredCreate(newLength);

            int arrayIndex = 0;
            for (int laneIndex = 0; laneIndex < m_Reader.LaneCount; ++laneIndex)
            {
                UnsafeTypedStream<T>.LaneReader laneReader = m_Reader.AsLaneReader(laneIndex);
                int elementCount = laneReader.Count;
                for (int elementIndex = 0; elementIndex < elementCount; ++elementIndex)
                {
                    array[arrayIndex] = laneReader.Read();
                    arrayIndex++;
                }
            }
        }
    }
}
