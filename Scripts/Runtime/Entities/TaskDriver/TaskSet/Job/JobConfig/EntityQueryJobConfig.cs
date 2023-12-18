namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class EntityQueryJobConfig : AbstractJobConfig
    {
        public EntityQueryJobConfig(ITaskSetOwner taskSetOwner, EntityQueryNativeList entityQueryNativeList)
            : base(taskSetOwner)
        {
            RequireEntityNativeArrayFromQueryForRead(entityQueryNativeList);
        }
    }
}