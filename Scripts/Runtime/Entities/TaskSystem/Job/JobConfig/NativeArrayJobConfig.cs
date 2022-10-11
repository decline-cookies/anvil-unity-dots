using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class NativeArrayJobConfig<T> : AbstractJobConfig
        where T : struct
    {
        public NativeArrayJobConfig(TaskFlowGraph taskFlowGraph,
                                    AbstractTaskSystem taskSystem,
                                    AbstractTaskDriver taskDriver,
                                    NativeArray<T> nativeArray)
            : base(taskFlowGraph,
                   taskSystem,
                   taskDriver)
        {
            RequireNativeArrayForRead(nativeArray);
        }
    }
}
