namespace Anvil.Unity.DOTS.Entities
{
    internal class EntityQueryJobConfig : AbstractJobConfig
    {
        public EntityQueryJobConfig(TaskFlowGraph taskFlowGraph,
                                    ITaskSystem taskSystem,
                                    ITaskDriver taskDriver,
                                    EntityQueryNativeArray entityQueryNativeArray)
            : base(taskFlowGraph,
                   taskSystem,
                   taskDriver)
        {
            RequireEntityNativeArrayFromQueryForRead(entityQueryNativeArray);
        }
    }
}
