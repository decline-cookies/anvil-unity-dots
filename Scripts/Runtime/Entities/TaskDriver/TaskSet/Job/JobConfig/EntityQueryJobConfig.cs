namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class EntityQueryJobConfig : AbstractJobConfig
    {
        public EntityQueryJobConfig(ITaskSetOwner taskSetOwner,
                                    EntityQueryNativeArray entityQueryNativeArray)
            : base(taskSetOwner)
        {
            RequireEntityNativeArrayFromQueryForRead(entityQueryNativeArray);
        }
    }
}
