using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public interface IJobConfigRequirements : IJobConfig
    {
        public IJobConfigRequirements RequireTaskStreamForWrite<TInstance>(TaskStream<TInstance> taskStream)
            where TInstance : unmanaged, IProxyInstance;
        
        public IJobConfigRequirements RequireTaskStreamForRead<TInstance>(TaskStream<TInstance> taskStream)
            where TInstance : unmanaged, IProxyInstance;

        public IJobConfigRequirements RequireNativeArrayForWrite<T>(NativeArray<T> array)
            where T : struct;

        public IJobConfigRequirements RequireNativeArrayForRead<T>(NativeArray<T> array)
            where T : struct;

        public IJobConfigRequirements RequireEntityNativeArrayFromQueryForRead(EntityQuery entityQuery);

        public IJobConfigRequirements RequireTaskDriverForRequestCancel(ITaskDriver taskDriver);
    }
}
