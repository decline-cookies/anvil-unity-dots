using System;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities
{
    internal class JobNodeLookup : AbstractNodeLookup
    {
        private static readonly TaskFlowRoute[] TASK_FLOW_ROUTE_VALUES = (TaskFlowRoute[])Enum.GetValues(typeof(TaskFlowRoute));
        
        private readonly Dictionary<TaskFlowRoute, JobRouteNode> m_JobRouteNodes;
        
        public JobNodeLookup(TaskFlowGraph taskFlowGraph, ITaskSystem taskSystem, ITaskDriver taskDriver) : base(taskFlowGraph, taskSystem, taskDriver)
        {
            m_JobRouteNodes = new Dictionary<TaskFlowRoute, JobRouteNode>();
        }

        protected override void DisposeSelf()
        {
            foreach (JobRouteNode node in m_JobRouteNodes.Values)
            {
                node.Dispose();
            }
            m_JobRouteNodes.Clear();
            
            base.DisposeSelf();
        }

        public void Harden()
        {
            foreach (JobRouteNode jobRouteNode in m_JobRouteNodes.Values)
            {
                jobRouteNode.Harden();
            }
        }

        public JobNode CreateJobNode(TaskFlowRoute route, AbstractJobConfig jobConfig)
        {
            JobRouteNode routeNode = GetOrCreateRouteNode(route);
            return routeNode.CreateNode(jobConfig);
        }

        private JobRouteNode GetOrCreateRouteNode(TaskFlowRoute route)
        {
            if (!m_JobRouteNodes.TryGetValue(route, out JobRouteNode routeNode))
            {
                routeNode = new JobRouteNode(this, 
                                             route, 
                                             TaskFlowGraph, 
                                             TaskSystem, 
                                             TaskDriver);
                m_JobRouteNodes.Add(route, routeNode);
            }

            return routeNode;
        }

        public void PopulateWithJobConfigs(Dictionary<TaskFlowRoute, List<AbstractJobConfig>> jobConfigs)
        {
            foreach (TaskFlowRoute route in TASK_FLOW_ROUTE_VALUES)
            {
                if (!jobConfigs.TryGetValue(route, out List<AbstractJobConfig> jobConfigList))
                {
                    jobConfigList = new List<AbstractJobConfig>();
                    jobConfigs.Add(route, jobConfigList);
                }
                JobRouteNode routeNode = GetOrCreateRouteNode(route);
                routeNode.PopulateWithJobConfigs(jobConfigList);
            }
        }
    }
}
