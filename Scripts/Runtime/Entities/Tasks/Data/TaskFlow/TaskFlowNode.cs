using Anvil.CSharp.Core;
using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

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
        
        private readonly Dictionary<Type, byte> m_ResolveChannelLookup;

        private readonly Dictionary<TaskFlowRoute, List<JobConfig>> m_JobConfigLookup;

        private readonly TaskFlowGraph m_TaskFlowGraph;

        public TaskFlowNode(TaskFlowGraph taskFlowGraph, AbstractProxyDataStream dataStream, ITaskSystem taskSystem, ITaskDriver taskDriver)
        {
            m_TaskFlowGraph = taskFlowGraph;
            DataStream = dataStream;
            TaskSystem = taskSystem;
            TaskDriver = taskDriver;
            DataOwner = TaskFlowOwner.System;
            
            if (taskDriver != null)
            {
                DataOwner = TaskFlowOwner.Driver;
            }

            m_JobConfigLookup = new Dictionary<TaskFlowRoute, List<JobConfig>>();

            m_ResolveChannelLookup = new Dictionary<Type, byte>();

            m_OutgoingNodesByRoute = new Dictionary<TaskFlowRoute, List<TaskFlowNode>>();
            m_IncomingNodesByRoute = new Dictionary<TaskFlowRoute, List<TaskFlowNode>>();
        }

        protected override void DisposeSelf()
        {
            DataStream.Dispose();
            base.DisposeSelf();
        }

        public override string ToString()
        {
            return $"{DataStream} located in {TaskDebugUtil.GetLocation(TaskSystem, TaskDriver)}";
        }

        public void RegisterJobConfig(TaskFlowRoute route, JobConfig jobConfig)
        {
            GetJobConfigsFor(route).Add(jobConfig);
        }


        public List<JobConfig> GetJobConfigsFor(TaskFlowRoute route)
        {
            if (!m_JobConfigLookup.TryGetValue(route, out List<JobConfig> configs))
            {
                configs = new List<JobConfig>();
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
            List<JobConfig> jobConfigs = GetJobConfigsFor(route);
            foreach (JobConfig jobConfig in jobConfigs)
            {
                //TODO: Get the data this JobConfig will operate on and in what context
            }
        }

        public void RegisterAsResolveChannel(ResolveChannelAttribute resolveChannelAttribute)
        {
            Type type = resolveChannelAttribute.ResolveChannel.GetType();
            byte value = (byte)resolveChannelAttribute.ResolveChannel;
            
            m_ResolveChannelLookup.Add(type, value);
        }

        public bool IsResolveChannel<TResolveChannel>(TResolveChannel resolveChannel)
            where TResolveChannel : Enum
        {
            Type type = typeof(TResolveChannel);
            if (!m_ResolveChannelLookup.TryGetValue(type, out byte value))
            {
                return false;
            }

            byte storedResolveChannel = UnsafeUtility.As<TResolveChannel, byte>(ref resolveChannel);
            return value == storedResolveChannel;
        }
    }
}
