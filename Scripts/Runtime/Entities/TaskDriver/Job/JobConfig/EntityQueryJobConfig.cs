namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class EntityQueryJobConfig : AbstractJobConfig
    {
        public EntityQueryJobConfig(TaskFlowGraph taskFlowGraph,
                                    AbstractTaskSet owningTaskSet,
                                    EntityQueryNativeArray entityQueryNativeArray)
            : base(taskFlowGraph,
                   owningTaskSet)
        {
            RequireEntityNativeArrayFromQueryForRead(entityQueryNativeArray);
        }
    }
}
