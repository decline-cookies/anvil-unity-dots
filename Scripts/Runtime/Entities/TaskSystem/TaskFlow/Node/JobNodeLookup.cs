using System;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class JobNodeLookup : AbstractNodeLookup
    {
        private static readonly TaskFlowRoute[] TASK_FLOW_ROUTE_VALUES = (TaskFlowRoute[])Enum.GetValues(typeof(TaskFlowRoute));

        private readonly Dictionary<TaskFlowRoute, JobRouteNode> m_JobRouteNodes;

        public JobNodeLookup(TaskFlowGraph taskGraph, AbstractTaskSystem taskSystem, AbstractTaskDriver taskDriver) : base(taskGraph, taskSystem, taskDriver)
        {
            m_JobRouteNodes = new Dictionary<TaskFlowRoute, JobRouteNode>();
        }

        public void CreateJobNode(TaskFlowRoute route, AbstractJobConfig jobConfig)
        {
            JobRouteNode routeNode = GetOrCreateRouteNode(route);
            routeNode.CreateNode(jobConfig);
        }

        private JobRouteNode GetOrCreateRouteNode(TaskFlowRoute route)
        {
            if (!m_JobRouteNodes.TryGetValue(route, out JobRouteNode routeNode))
            {
                routeNode = new JobRouteNode(this,
                                             route,
                                             TaskGraph,
                                             TaskSystem,
                                             TaskDriver);
                m_JobRouteNodes.Add(route, routeNode);
            }

            return routeNode;
        }

        public void AddJobConfigsTo(Dictionary<TaskFlowRoute, List<AbstractJobConfig>> jobConfigs)
        {
            foreach (TaskFlowRoute route in TASK_FLOW_ROUTE_VALUES)
            {
                if (!jobConfigs.TryGetValue(route, out List<AbstractJobConfig> jobConfigList))
                {
                    jobConfigList = new List<AbstractJobConfig>();
                    jobConfigs.Add(route, jobConfigList);
                }

                JobRouteNode routeNode = GetOrCreateRouteNode(route);
                routeNode.AddJobConfigsTo(jobConfigList);
            }
        }
    }
}
