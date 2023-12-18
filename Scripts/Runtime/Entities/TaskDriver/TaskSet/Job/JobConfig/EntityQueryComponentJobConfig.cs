using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class EntityQueryComponentJobConfig<T> : AbstractJobConfig where T : unmanaged, IComponentData
    {
        public EntityQueryComponentJobConfig(ITaskSetOwner taskSetOwner, EntityQueryComponentNativeList<T> entityQueryComponentNativeList)
            : base(taskSetOwner)
        {
            RequireIComponentDataNativeArrayFromQueryForRead(entityQueryComponentNativeList);
        }
    }
}