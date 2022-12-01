namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class EntityQueryJobConfig : AbstractJobConfig
    {
        public EntityQueryJobConfig(TaskFlowGraph taskFlowGraph,
                                    AbstractWorkload owningWorkload,
                                    EntityQueryNativeArray entityQueryNativeArray)
            : base(taskFlowGraph,
                   owningWorkload)
        {
            RequireEntityNativeArrayFromQueryForRead(entityQueryNativeArray);
        }
    }
}
