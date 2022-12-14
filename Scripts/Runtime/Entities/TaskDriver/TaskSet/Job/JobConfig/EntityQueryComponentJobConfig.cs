using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class EntityQueryComponentJobConfig<T> : AbstractJobConfig
        where T : struct, IComponentData
    {
        public EntityQueryComponentJobConfig(ITaskSetOwner taskSetOwner,
                                             EntityQueryComponentNativeArray<T> entityQueryComponentNativeArray)
            : base(taskSetOwner)
        {
            RequireIComponentDataNativeArrayFromQueryForRead(entityQueryComponentNativeArray);
        }
    }
}
