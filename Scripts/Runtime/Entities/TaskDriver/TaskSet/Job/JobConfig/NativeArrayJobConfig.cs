using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class NativeArrayJobConfig<T> : AbstractJobConfig
        where T : struct
    {
        public NativeArrayJobConfig(ITaskSetOwner taskSetOwner,
                                    AccessControlledValue<NativeArray<T>> nativeArray)
            : base(taskSetOwner)
        {
            RequireGenericDataForRead(nativeArray);
        }
    }
}
