using Anvil.Unity.DOTS.Data;
using System;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities
{
    internal class NativeArrayScheduleWrapper<T> : IScheduleWrapper
        where T : struct
    {
        public int BatchSize
        {
            get;
        }

        public int Length
        {
            get => m_Array.Length;
        }

        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo
        {
            get => throw new NotSupportedException();
        }

        private readonly NativeArray<T> m_Array;
        public NativeArrayScheduleWrapper(NativeArray<T> array, BatchStrategy batchStrategy)
        {
            m_Array = array;
            
            BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
                ? ChunkUtil.MaxElementsPerChunk<T>()
                : 1;
        }
    }
}
