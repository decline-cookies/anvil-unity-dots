using Anvil.Unity.DOTS.Data;
using System;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities
{
    internal class NativeArrayScheduleInfo<T> : IScheduleInfo
        where T : unmanaged
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
        public NativeArrayScheduleInfo(NativeArray<T> array, BatchStrategy batchStrategy)
        {
            m_Array = array;
            
            BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
                ? ChunkUtil.MaxElementsPerChunk<T>()
                : 1;
        }
    }
}
