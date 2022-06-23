using Anvil.Unity.DOTS.Data;
using System;

namespace Anvil.Unity.DOTS.Entities
{
    internal class VirtualDataScheduleInfo<TKey, TInstance> : IScheduleInfo
        where TKey : struct, IEquatable<TKey>
        where TInstance : struct, IKeyedData<TKey>
    {
        public int BatchSize
        {
            get;
        }

        public int Length
        {
            get => throw new NotSupportedException();
        }

        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo
        {
            get;
        }

        public VirtualDataScheduleInfo(VirtualData<TKey, TInstance> data, BatchStrategy batchStrategy)
        {
            DeferredNativeArrayScheduleInfo = data.ScheduleInfo;

            BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
                ? VirtualData<TKey, TInstance>.MAX_ELEMENTS_PER_CHUNK
                : 1;
        }
    }
}
