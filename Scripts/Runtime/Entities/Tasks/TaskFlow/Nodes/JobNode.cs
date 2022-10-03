namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class JobNode : AbstractNode
    {
        private readonly JobRouteNode m_RouteNode;
        
        public TaskFlowRoute Route
        {
            get;
        }

        public AbstractJobConfig JobConfig
        {
            get;
        }

        public JobNode(JobRouteNode routeNode,
                       TaskFlowRoute route,
                       AbstractJobConfig jobConfig,
                       TaskFlowGraph taskFlowGraph,
                       ITaskSystem taskSystem,
                       ITaskDriver taskDriver) : base(taskFlowGraph, taskSystem, taskDriver)
        {
            m_RouteNode = routeNode;
            Route = route;
            JobConfig = jobConfig;
        }

        protected override void DisposeSelf()
        {
            JobConfig.Dispose();
            base.DisposeSelf();
        }

        public void Harden()
        {
            JobConfig.Harden();
        }
    }
}
