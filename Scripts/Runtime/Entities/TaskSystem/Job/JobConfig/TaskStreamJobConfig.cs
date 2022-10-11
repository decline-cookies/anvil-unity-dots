namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class TaskStreamJobConfig<TInstance> : AbstractJobConfig
        where TInstance : unmanaged, IEntityProxyInstance
    {
        public TaskStreamJobConfig(TaskFlowGraph taskFlowGraph,
                                   AbstractTaskSystem taskSystem,
                                   AbstractTaskDriver taskDriver,
                                   TaskStream<TInstance> taskStream)
            : base(taskFlowGraph,
                   taskSystem,
                   taskDriver)
        {
            RequireTaskStreamForRead(taskStream);
        }
    }
}
