namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class TaskStreamJobConfig<TInstance> : AbstractJobConfig
        where TInstance : unmanaged, IProxyInstance
    {
        public TaskStreamJobConfig(TaskFlowGraph taskFlowGraph,
                                   ITaskSystem taskSystem,
                                   ITaskDriver taskDriver,
                                   TaskStream<TInstance> taskStream)
            : base(taskFlowGraph,
                   taskSystem,
                   taskDriver)
        {
            RequireTaskStreamForRead(taskStream);
        }
    }
}
