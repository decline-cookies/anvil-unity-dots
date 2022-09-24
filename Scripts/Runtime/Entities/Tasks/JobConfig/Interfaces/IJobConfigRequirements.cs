using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IJobConfigRequirements
    {
        public IJobConfigRequirements RequireTaskStreamForWrite<TInstance>(ITaskStream<TInstance> taskStream)
            where TInstance : unmanaged, IProxyInstance;
        
        public IJobConfigRequirements RequireTaskStreamForRead<TInstance>(ITaskStream<TInstance> taskStream)
            where TInstance : unmanaged, IProxyInstance;

        public IJobConfigRequirements RequireNativeArrayForWrite<T>(NativeArray<T> array)
            where T : unmanaged;

        public IJobConfigRequirements RequireNativeArrayForRead<T>(NativeArray<T> array)
            where T : unmanaged;

        public IJobConfigRequirements RequireEntityNativeArrayFromQueryForRead(EntityQuery entityQuery);
    }
}
