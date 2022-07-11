using Anvil.Unity.DOTS.Data;
using System;

namespace Anvil.Unity.DOTS.Entities
{
    internal class VirtualDataScheduleInfo<TInstance> : IScheduleInfo
        where TInstance : unmanaged, IKeyedData
    {
        public int BatchSize
        {
            get;
        }

        public int Length
        {
            get => throw new NotSupportedException($"This scheduling info is based on {nameof(VirtualData<TInstance>)} which uses {nameof(DeferredNativeArray<TInstance>)}. The {nameof(Length)} is not known at schedule time as it will be filled in by a later job.");
        }

        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo
        {
            get;
        }

        public VirtualDataScheduleInfo(VirtualData<TInstance> data, BatchStrategy batchStrategy, bool isCancel)
        {
            DeferredNativeArrayScheduleInfo = isCancel ? data.CancelScheduleInfo :  data.ScheduleInfo;

            BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
                ? VirtualData<TInstance>.MAX_ELEMENTS_PER_CHUNK
                : 1;
        }
    }
}
