using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IJobConfigScheduling
    {
        public IJobConfigRequirements ScheduleOn<TInstance>(ITaskStream<TInstance> taskStream, BatchStrategy batchStrategy)
            where TInstance : unmanaged, IProxyInstance;

        public IJobConfigRequirements ScheduleOn<T>(NativeArray<T> nativeArray, BatchStrategy batchStrategy)
            where T : unmanaged;

        public IJobConfigRequirements ScheduleOn(EntityQuery entityQuery, BatchStrategy batchStrategy);
    }
}
