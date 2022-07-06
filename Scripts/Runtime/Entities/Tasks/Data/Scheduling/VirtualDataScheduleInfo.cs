using Anvil.Unity.DOTS.Data;
using System;

namespace Anvil.Unity.DOTS.Entities
{
    internal class VirtualDataScheduleInfo<TKey, TInstance> : IScheduleInfo
        where TKey : unmanaged, IEquatable<TKey>
        where TInstance : unmanaged, IKeyedData<TKey>
    {
        public int BatchSize
        {
            get;
        }

        public int Length
        {
            get => throw new NotSupportedException($"This scheduling info is based on {nameof(VirtualData<TKey, TInstance>)} which uses {nameof(DeferredNativeArray<TInstance>)}. The {nameof(Length)} is not known at schedule time as it will be filled in by a later job.");
        }

        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo
        {
            get;
        }

        public VirtualDataScheduleInfo(VirtualData<TKey, TInstance> data, BatchStrategy batchStrategy, bool isForCancel)
        {
            DeferredNativeArrayScheduleInfo = isForCancel ? data.ScheduleInfo : data.CancelScheduleInfo;

            BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
                ? VirtualData<TKey, TInstance>.MAX_ELEMENTS_PER_CHUNK
                : 1;
        }
    }
}
