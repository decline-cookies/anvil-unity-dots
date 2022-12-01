using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class EntityQueryComponentJobConfig<T> : AbstractJobConfig
        where T : struct, IComponentData
    {
        public EntityQueryComponentJobConfig(TaskFlowGraph taskFlowGraph,
                                             AbstractWorkload owningWorkload,
                                             EntityQueryComponentNativeArray<T> entityQueryComponentNativeArray)
            : base(taskFlowGraph,
                   owningWorkload)
        {
            RequireIComponentDataNativeArrayFromQueryForRead(entityQueryComponentNativeArray);
        }
    }
}
