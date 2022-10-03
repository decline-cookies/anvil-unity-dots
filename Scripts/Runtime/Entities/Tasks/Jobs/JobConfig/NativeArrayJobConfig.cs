using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class NativeArrayJobConfig<T> : AbstractJobConfig
        where T : struct
    {
        public NativeArrayJobConfig(TaskFlowGraph taskFlowGraph,
                                    ITaskSystem taskSystem,
                                    ITaskDriver taskDriver,
                                    NativeArray<T> nativeArray)
            : base(taskFlowGraph,
                   taskSystem,
                   taskDriver)
        {
            RequireNativeArrayForRead(nativeArray);
        }
    }
}
