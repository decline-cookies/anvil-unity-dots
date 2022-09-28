using Anvil.Unity.DOTS.Data;
using System;

namespace Anvil.Unity.DOTS.Entities
{
    public class CancelTaskStreamScheduleInfo<TInstance> : IScheduleInfo
        where TInstance : unmanaged, IProxyInstance
    {

        internal DataStreamCancellationUpdater<TInstance> CancellationUpdater { get; private set; }
        public int BatchSize { get; }
        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo { get; }
        
        public int Length
        {
            get => throw new NotSupportedException($"This scheduling info is based on {nameof(ProxyDataStream<TInstance>)} which uses {nameof(DeferredNativeArray<TInstance>)}. The {nameof(Length)} is not known at schedule time as it will be filled in by a later job.");
        }
        
        public CancelTaskStreamScheduleInfo(ProxyDataStream<TInstance> data, BatchStrategy batchStrategy)
        {
            DeferredNativeArrayScheduleInfo = data.ScheduleInfo;

            BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
                ? ProxyDataStream<TInstance>.MAX_ELEMENTS_PER_CHUNK
                : 1;
        }

        internal void SetCancellationUpdater(DataStreamCancellationUpdater<TInstance> cancellationUpdater)
        {
            CancellationUpdater = cancellationUpdater;
        }
    }
}
