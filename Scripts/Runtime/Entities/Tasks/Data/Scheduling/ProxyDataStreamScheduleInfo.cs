using Anvil.Unity.DOTS.Data;
using System;

namespace Anvil.Unity.DOTS.Entities
{
    internal class ProxyDataStreamScheduleInfo<TData> : IScheduleInfo
        where TData : unmanaged, IProxyData
    {
        public int BatchSize
        {
            get;
        }

        public int Length
        {
            get => throw new NotSupportedException($"This scheduling info is based on {nameof(ProxyDataStream<TData>)} which uses {nameof(DeferredNativeArray<TData>)}. The {nameof(Length)} is not known at schedule time as it will be filled in by a later job.");
        }

        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo
        {
            get;
        }

        public ProxyDataStreamScheduleInfo(ProxyDataStream<TData> data, BatchStrategy batchStrategy)
        {
            DeferredNativeArrayScheduleInfo = data.ScheduleInfo;

            BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
                ? ProxyDataStream<TData>.MAX_ELEMENTS_PER_CHUNK
                : 1;
        }
    }
}
