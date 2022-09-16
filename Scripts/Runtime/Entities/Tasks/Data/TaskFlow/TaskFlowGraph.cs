using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    internal class TaskFlowGraph
    {
        public static readonly TaskFlowRoute[] TASK_FLOW_ROUTE_VALUES = (TaskFlowRoute[])Enum.GetValues(typeof(TaskFlowRoute));
        
        private readonly Dictionary<IProxyDataStream, TaskFlowNode> m_NodesLookupByDataStream;
        private readonly Dictionary<ITaskSystem, List<TaskFlowNode>> m_NodesLookupOwnedByTaskSystem;
        private readonly Dictionary<ITaskDriver, List<TaskFlowNode>> m_NodesLookupOwnedByTaskDriver;
        private bool m_IsHardened;

        public TaskFlowGraph()
        {
            m_NodesLookupByDataStream = new Dictionary<IProxyDataStream, TaskFlowNode>();
            m_NodesLookupOwnedByTaskSystem = new Dictionary<ITaskSystem, List<TaskFlowNode>>();
            m_NodesLookupOwnedByTaskDriver = new Dictionary<ITaskDriver, List<TaskFlowNode>>();
        }

        public void RegisterDataStream(IProxyDataStream dataStream, ITaskSystem taskSystem, ITaskDriver taskDriver)
        {
            Debug_EnsureNotHardened();
            Debug_EnsureNoDuplicateNodes(dataStream);

            TaskFlowNode node = new TaskFlowNode(this,
                                                 dataStream,
                                                 taskSystem,
                                                 taskDriver);
            m_NodesLookupByDataStream.Add(dataStream, node);
            if (taskDriver == null)
            {
                GetOrCreateNodeList(taskSystem, m_NodesLookupOwnedByTaskSystem).Add(node);
            }
            else
            {
                GetOrCreateNodeList(taskDriver, m_NodesLookupOwnedByTaskDriver).Add(node);
            }
        }

        public IJobConfig CreateJobConfig<TInstance>(World world,
                                                     ProxyDataStream<TInstance> dataStream,
                                                     JobConfig<TInstance>.ScheduleJobDelegate scheduleJobFunction,
                                                     BatchStrategy batchStrategy,
                                                     TaskFlowRoute route)
            where TInstance : unmanaged, IProxyInstance
        {
            Debug_EnsureExists(dataStream);
            TaskFlowNode node = this[dataStream];
            
            JobConfig<TInstance> jobConfig = new JobConfig<TInstance>(world,
                                                                      node.TaskDriver?.Context ?? node.TaskSystem.Context,
                                                                      scheduleJobFunction,
                                                                      batchStrategy,
                                                                      dataStream);

            node.RegisterJobConfig(route, jobConfig);

            return jobConfig;
        }

        public bool IsDataStreamRegistered(IProxyDataStream dataStream)
        {
            return m_NodesLookupByDataStream.ContainsKey(dataStream);
        }

        public void DisposeFor(ITaskSystem system)
        {
            List<TaskFlowNode> nodes = GetOrCreateNodeList(system, m_NodesLookupOwnedByTaskSystem);
            DisposeNodes(nodes);
        }

        public void DisposeFor(ITaskDriver driver)
        {
            List<TaskFlowNode> nodes = GetOrCreateNodeList(driver, m_NodesLookupOwnedByTaskDriver);
            DisposeNodes(nodes);
        }

        public string GetDebugString(IProxyDataStream dataStream)
        {
            Debug_EnsureExists(dataStream);
            return m_NodesLookupByDataStream[dataStream].GetDebugString();
        }

        private void DisposeNodes(List<TaskFlowNode> nodes)
        {
            foreach (TaskFlowNode node in nodes)
            {
                node.Dispose();
            }
        }


        private List<TaskFlowNode> GetOrCreateNodeList<TKey>(TKey key, Dictionary<TKey, List<TaskFlowNode>> dictionary)
        {
            if (!dictionary.TryGetValue(key, out List<TaskFlowNode> nodes))
            {
                nodes = new List<TaskFlowNode>();
                dictionary.Add(key, nodes);
            }

            return nodes;
        }


        public TaskFlowNode this[IProxyDataStream dataStream]
        {
            get => m_NodesLookupByDataStream[dataStream];
        }

        public void Harden()
        {
            if (m_IsHardened)
            {
                return;
            }

            m_IsHardened = true;

            //Iterate through all nodes registered to the graph to try and develop relationships. 
            //We'll end up getting islands of relationships between all the data so you can't necessarily have 
            //one entry and one exit.
            foreach (TaskFlowNode node in m_NodesLookupByDataStream.Values)
            {
                node.BuildConnections();
            }

            Debug_EnsureJobFlowIsComplete();
        }

        public BulkJobScheduler<IProxyDataStream> CreateDataStreamBulkJobSchedulerFor(ITaskSystem system)
        {
            List<IProxyDataStream> dataStreams = new List<IProxyDataStream>();

            List<TaskFlowNode> nodes = m_NodesLookupOwnedByTaskSystem[system];
            foreach (TaskFlowNode node in nodes)
            {
                dataStreams.Add(node.DataStream);
            }

            return new BulkJobScheduler<IProxyDataStream>(dataStreams);
        }

        public BulkJobScheduler<IProxyDataStream> CreateDataStreamBulkJobSchedulerFor<TTaskDriver>(List<TTaskDriver> taskDrivers)
            where TTaskDriver : class, ITaskDriver
        {
            List<IProxyDataStream> dataStreams = new List<IProxyDataStream>();

            foreach (TTaskDriver driver in taskDrivers)
            {
                List<TaskFlowNode> nodes = m_NodesLookupOwnedByTaskDriver[driver];
                foreach (TaskFlowNode node in nodes)
                {
                    dataStreams.Add(node.DataStream);
                }
            }

            return new BulkJobScheduler<IProxyDataStream>(dataStreams);
        }

        public Dictionary<TaskFlowRoute, BulkJobScheduler<IJobConfig>> CreateJobConfigBulkJobSchedulerLookupFor(ITaskSystem system)
        {
            Dictionary<TaskFlowRoute, BulkJobScheduler<IJobConfig>> lookup = new Dictionary<TaskFlowRoute, BulkJobScheduler<IJobConfig>>();

            List<TaskFlowNode> nodes = m_NodesLookupOwnedByTaskSystem[system];
            foreach (TaskFlowRoute route in TASK_FLOW_ROUTE_VALUES)
            {
                lookup.Add(route, CreateJobConfigBulkJobScheduler(route, nodes));
            }

            return lookup;
        }

        public Dictionary<TaskFlowRoute, BulkJobScheduler<IJobConfig>> CreateJobConfigBulkJobSchedulerLookupFor<TTaskDriver>(List<TTaskDriver> taskDrivers)
            where TTaskDriver : class, ITaskDriver
        {
            List<TaskFlowNode> nodes = new List<TaskFlowNode>();
            
            foreach (TTaskDriver driver in taskDrivers)
            {
                List<TaskFlowNode> taskDriverNodes = m_NodesLookupOwnedByTaskDriver[driver];
                nodes.AddRange(taskDriverNodes);
            }
            
            Dictionary<TaskFlowRoute, BulkJobScheduler<IJobConfig>> lookup = new Dictionary<TaskFlowRoute, BulkJobScheduler<IJobConfig>>();
            foreach (TaskFlowRoute route in TASK_FLOW_ROUTE_VALUES)
            {
                lookup.Add(route, CreateJobConfigBulkJobScheduler(route, nodes));
            }

            return lookup;
        }

        private BulkJobScheduler<IJobConfig> CreateJobConfigBulkJobScheduler(TaskFlowRoute route, List<TaskFlowNode> nodes)
        {
            List<IJobConfig> jobConfigs = new List<IJobConfig>();
            foreach (TaskFlowNode node in nodes)
            {
                jobConfigs.AddRange(node.GetJobConfigsFor(route));
            }
            return new BulkJobScheduler<IJobConfig>(jobConfigs);
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureExists(IProxyDataStream dataStream)
        {
            if (!m_NodesLookupByDataStream.ContainsKey(dataStream))
            {
                throw new InvalidOperationException($"Trying to access a {nameof(TaskFlowNode)} with instance of {dataStream.DebugString} but it doesn't exist!");
            }
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNoDuplicateNodes(IProxyDataStream dataStream)
        {
            if (m_NodesLookupByDataStream.ContainsKey(dataStream))
            {
                throw new InvalidOperationException($"Trying to create a new {nameof(TaskFlowNode)} with instance of {dataStream.DebugString} but one already exists!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNotHardened()
        {
            if (m_IsHardened)
            {
                throw new InvalidOperationException($"Trying to modify the {nameof(TaskFlowGraph)} but connections have already been built! The graph needs to be complete before connections are built.");
            }
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureJobFlowIsComplete()
        {
            //TODO: Ensure that all data is written to somehow and used
            // foreach (DataFlowNode node in m_DataFlowNodes.Values)
            // {
            //     foreach (DataFlowNode.DataFlowPath path in DataFlowNode.FlowPathValues)
            //     {
            //         if (node.HasJobsFor(path))
            //         {
            //             continue;
            //         }
            //
            //         string issue = path switch
            //         {
            //             DataFlowNode.DataFlowPath.Populate => $"The data will never be populated with any instances.",
            //             DataFlowNode.DataFlowPath.Update   => $"There will be data loss as this data will never be updated to flow to a results location or continue in the stream.",
            //             _                                  => throw new ArgumentOutOfRangeException($"Tried to generate issue string for {path} but no code path satisfies!")
            //         };
            //         
            //         throw new InvalidOperationException($"{node.DataStream.GetType()} located on {node.ToLocationString()}, does not have any job for {path}. {issue}");
            //     }
            // }
        }
    }
}
