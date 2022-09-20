using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Anvil.Unity.DOTS.Entities
{
    internal class JobRouteNode : AbstractNode
    {
        private readonly Dictionary<JobConfig, JobNode> m_JobsByConfig;
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
            m_JobsByConfig = new Dictionary<JobConfig, JobNode>();
        }

        public JobNode CreateNode(JobConfig jobConfig)
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

        public void PopulateWithJobConfigs(List<JobConfig> jobConfigs)
        {
            foreach (JobConfig jobConfig in m_JobsByConfig.Keys)
            {
                jobConfigs.Add(jobConfig);
            }
        }


        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNoDuplicateNodes(JobConfig config)
        {
            if (m_JobsByConfig.ContainsKey(config))
            {
                throw new InvalidOperationException($"Trying to create a new {nameof(JobNode)} with config of {config} but one already exists!");
            }
        }
    }
}
