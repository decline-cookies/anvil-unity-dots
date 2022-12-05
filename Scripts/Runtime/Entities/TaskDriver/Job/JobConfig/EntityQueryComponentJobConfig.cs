using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class EntityQueryComponentJobConfig<T> : AbstractJobConfig
        where T : struct, IComponentData
    {
        public EntityQueryComponentJobConfig(TaskFlowGraph taskFlowGraph,
                                             AbstractTaskSet owningTaskSet,
                                             EntityQueryComponentNativeArray<T> entityQueryComponentNativeArray)
            : base(taskFlowGraph,
                   owningTaskSet)
        {
            RequireIComponentDataNativeArrayFromQueryForRead(entityQueryComponentNativeArray);
        }
    }
}
