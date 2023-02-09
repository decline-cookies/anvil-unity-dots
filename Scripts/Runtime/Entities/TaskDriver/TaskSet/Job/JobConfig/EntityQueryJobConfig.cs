namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class EntityQueryJobConfig : AbstractJobConfig
    {
        public EntityQueryJobConfig(ITaskSetOwner taskSetOwner, EntityQueryNativeArray entityQueryNativeArray)
            : base(taskSetOwner)
        {
            RequireEntityNativeArrayFromQueryForRead(entityQueryNativeArray);
        }
    }
}