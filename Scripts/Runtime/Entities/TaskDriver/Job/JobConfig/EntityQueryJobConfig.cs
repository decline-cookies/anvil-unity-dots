namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class EntityQueryJobConfig : AbstractJobConfig
    {
        public EntityQueryJobConfig(TaskFlowGraph taskFlowGraph,
                                    AbstractTaskDriverSystem taskSystem,
                                    AbstractTaskDriver taskDriver,
                                    EntityQueryNativeArray entityQueryNativeArray)
            : base(taskFlowGraph,
                   taskSystem,
                   taskDriver)
        {
            RequireEntityNativeArrayFromQueryForRead(entityQueryNativeArray);
        }
    }
}
