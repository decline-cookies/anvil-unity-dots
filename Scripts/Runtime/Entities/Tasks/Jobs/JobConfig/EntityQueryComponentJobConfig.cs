using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class EntityQueryComponentJobConfig<T> : AbstractJobConfig
        where T : struct, IComponentData
    {
        public EntityQueryComponentJobConfig(TaskFlowGraph taskFlowGraph,
                                             ITaskSystem taskSystem,
                                             ITaskDriver taskDriver,
                                             EntityQueryComponentNativeArray<T> entityQueryComponentNativeArray)
            : base(taskFlowGraph,
                   taskSystem,
                   taskDriver)
        {
            RequireIComponentDataNativeArrayFromQueryForRead(entityQueryComponentNativeArray);
        }
    }
}
