using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class JobRouteNode : AbstractNode
    {
        private readonly Dictionary<AbstractJobConfig, JobNode> m_JobsByConfig;
        private readonly JobNodeLookup m_Lookup;

        public TaskFlowRoute Route
        {
            get;
        }

        public JobRouteNode(JobNodeLookup lookup,
                            TaskFlowRoute route,
                            TaskFlowGraph taskFlowGraph,
                            ITaskSystem taskSystem,
                            ITaskDriver taskDriver) : base(taskFlowGraph, taskSystem, taskDriver)
        {
            m_Lookup = lookup;
            Route = route;
            m_JobsByConfig = new Dictionary<AbstractJobConfig, JobNode>();
        }

        protected override void DisposeSelf()
        {
            foreach (JobNode node in m_JobsByConfig.Values)
            {
                node.Dispose();
            }
            m_JobsByConfig.Clear();
            
            base.DisposeSelf();
        }

        public void Harden()
        {
            foreach (JobNode node in m_JobsByConfig.Values)
            {
                node.Harden();
            }
        }

        public JobNode CreateNode(AbstractJobConfig jobConfig)
        {
            Debug_EnsureNoDuplicateNodes(jobConfig);
            JobNode node = new JobNode(this,
                                       Route,
                                       jobConfig,
                                       TaskFlowGraph,
                                       TaskSystem,
                                       TaskDriver);
            m_JobsByConfig.Add(jobConfig, node);
            return node;
        }

        public void PopulateWithJobConfigs(List<AbstractJobConfig> jobConfigs)
        {
            foreach (AbstractJobConfig jobConfig in m_JobsByConfig.Keys)
            {
                jobConfigs.Add(jobConfig);
            }
        }


        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNoDuplicateNodes(AbstractJobConfig config)
        {
            if (m_JobsByConfig.ContainsKey(config))
            {
                throw new InvalidOperationException($"Trying to create a new {nameof(JobNode)} with config of {config} but one already exists!");
            }
        }
    }
}
