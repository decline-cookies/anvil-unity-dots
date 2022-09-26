using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IScheduleJobConfig
    {
        public IJobConfig ScheduleOn<TInstance>(ProxyDataStream<TInstance> dataStream, BatchStrategy batchStrategy)
            where TInstance : unmanaged, IProxyInstance;

        public IJobConfig ScheduleOn<T>(NativeArray<T> nativeArray, BatchStrategy batchStrategy)
            where T : unmanaged;

        public IJobConfig ScheduleOn(EntityQuery entityQuery, BatchStrategy batchStrategy);
    }
}
