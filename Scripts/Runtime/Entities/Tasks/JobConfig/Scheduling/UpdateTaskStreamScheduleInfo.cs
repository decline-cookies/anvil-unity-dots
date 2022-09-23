using Anvil.Unity.DOTS.Data;
using System;

namespace Anvil.Unity.DOTS.Entities
{
    public class UpdateTaskStreamScheduleInfo<TInstance> : IScheduleInfo
        where TInstance : unmanaged, IProxyInstance
    {

        internal DataStreamUpdater<TInstance> Updater;

        public int BatchSize
        {
            get;
        }

        public int Length
        {
            get => throw new NotSupportedException($"This scheduling info is based on {nameof(ProxyDataStream<TInstance>)} which uses {nameof(DeferredNativeArray<TInstance>)}. The {nameof(Length)} is not known at schedule time as it will be filled in by a later job.");
        }

        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo
        {
            get;
        }

        public UpdateTaskStreamScheduleInfo(ProxyDataStream<TInstance> data, BatchStrategy batchStrategy)
        {
            DeferredNativeArrayScheduleInfo = data.ScheduleInfo;

            BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
                ? ProxyDataStream<TInstance>.MAX_ELEMENTS_PER_CHUNK
                : 1;
        }
    }
}
