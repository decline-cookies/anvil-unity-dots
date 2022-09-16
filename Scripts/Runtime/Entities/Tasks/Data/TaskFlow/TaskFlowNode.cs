using Anvil.CSharp.Core;
using System;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities
{
    internal class TaskFlowNode : AbstractAnvilBase
    {
        public AbstractProxyDataStream DataStream
        {
            get;
        }

        public ITaskSystem TaskSystem
        {
            get;
        }

        public ITaskDriver TaskDriver
        {
            get;
        }

        public TaskFlowOwner DataOwner
        {
            get;
        }

        private readonly Dictionary<TaskFlowRoute, List<TaskFlowNode>> m_OutgoingNodesByRoute;
        private readonly Dictionary<TaskFlowRoute, List<TaskFlowNode>> m_IncomingNodesByRoute;

        private readonly Dictionary<TaskFlowRoute, List<AbstractJobConfig>> m_JobConfigLookup;

        private readonly Type m_TaskSystemType;
        private readonly Type m_TaskDriverType;

        private readonly TaskFlowGraph m_TaskFlowGraph;

        public TaskFlowNode(TaskFlowGraph taskFlowGraph, AbstractProxyDataStream dataStream, ITaskSystem taskSystem, ITaskDriver taskDriver)
        {
            m_TaskFlowGraph = taskFlowGraph;
            DataStream = dataStream;
            TaskSystem = taskSystem;
            TaskDriver = taskDriver;
            DataOwner = TaskFlowOwner.System;
            
            m_TaskSystemType = TaskSystem.GetType();
            
            if (taskDriver != null)
            {
                DataOwner = TaskFlowOwner.Driver;
                m_TaskDriverType = TaskDriver.GetType();
            }

            m_JobConfigLookup = new Dictionary<TaskFlowRoute, List<AbstractJobConfig>>();
            
            m_OutgoingNodesByRoute = new Dictionary<TaskFlowRoute, List<TaskFlowNode>>();
            m_IncomingNodesByRoute = new Dictionary<TaskFlowRoute, List<TaskFlowNode>>();
        }

        protected override void DisposeSelf()
        {
            DataStream.Dispose();
            base.DisposeSelf();
        }

        public void RegisterJobConfig(TaskFlowRoute route, AbstractJobConfig jobConfig)
        {
            GetJobConfigsFor(route).Add(jobConfig);
        }


        public List<AbstractJobConfig> GetJobConfigsFor(TaskFlowRoute route)
        {
            if (!m_JobConfigLookup.TryGetValue(route, out List<AbstractJobConfig> configs))
            {
                configs = new List<AbstractJobConfig>();
                m_JobConfigLookup.Add(route, configs);
            }

            return configs;
        }

        public void BuildConnections()
        {
            foreach (TaskFlowRoute route in TaskFlowGraph.TASK_FLOW_ROUTE_VALUES)
            {
                BuildConnectionsForRoute(route);
            }
        }

        private void BuildConnectionsForRoute(TaskFlowRoute route)
        {
            List<AbstractJobConfig> jobConfigs = GetJobConfigsFor(route);
            foreach (AbstractJobConfig jobConfig in jobConfigs)
            {
                //TODO: Get the data this JobConfig will operate on and in what context
                
            }
        }


        public string GetDebugString()
        {
            string location = (TaskDriver == null)
                ? $"{m_TaskSystemType.Name}"
                : $"{m_TaskDriverType.Name} as part of the {m_TaskSystemType.Name} system";

            return $"${DataStream.DebugString} located in {location}";
        }
    }
}
