using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Anvil.Unity.DOTS.Entities
{
    internal class TaskFlowGraph : ITaskFlowGraph
    {
        public static readonly TaskFlowRoute[] TASK_FLOW_ROUTE_VALUES = (TaskFlowRoute[])Enum.GetValues(typeof(TaskFlowRoute));
        private static readonly Type ABSTRACT_PROXY_DATA_STREAM_TYPE = typeof(AbstractProxyDataStream);

        private readonly Dictionary<AbstractProxyDataStream, TaskFlowNode> m_NodesLookupByDataStream;
        private readonly Dictionary<ITaskSystem, List<TaskFlowNode>> m_NodesLookupOwnedByTaskSystem;
        private readonly Dictionary<ITaskDriver, List<TaskFlowNode>> m_NodesLookupOwnedByTaskDriver;
        private readonly Dictionary<ITaskSystem, List<ITaskDriver>> m_TaskDriversByTaskSystem;
        private bool m_IsHardened;

        public TaskFlowGraph()
        {
            m_NodesLookupByDataStream = new Dictionary<AbstractProxyDataStream, TaskFlowNode>();
            m_NodesLookupOwnedByTaskSystem = new Dictionary<ITaskSystem, List<TaskFlowNode>>();
            m_NodesLookupOwnedByTaskDriver = new Dictionary<ITaskDriver, List<TaskFlowNode>>();
            m_TaskDriversByTaskSystem = new Dictionary<ITaskSystem, List<ITaskDriver>>();
        }

        public void CreateDataStreams(ITaskSystem taskSystem, ITaskDriver taskDriver = null)
        {
            Debug_EnsureNotHardened();
            RegisterTaskDriverToTaskSystem(taskSystem, taskDriver);

            Type type;
            object instance;
            if (taskDriver == null)
            {
                type = taskSystem.GetType();
                instance = taskSystem;
            }
            else
            {
                type = taskDriver.GetType();
                instance = taskDriver;
            }

            //Get all the fields
            FieldInfo[] systemFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo systemField in systemFields)
            {
                if (!ABSTRACT_PROXY_DATA_STREAM_TYPE.IsAssignableFrom(systemField.FieldType))
                {
                    continue;
                }

                IgnoreProxyDataStreamAttribute ignoreProxyDataStreamAttribute = systemField.GetCustomAttribute<IgnoreProxyDataStreamAttribute>();
                if (ignoreProxyDataStreamAttribute != null)
                {
                    continue;
                }

                Debug_CheckFieldIsReadOnly(systemField);
                Debug_CheckFieldTypeGenericTypeArguments(systemField.FieldType);

                //Get the data type 
                AbstractProxyDataStream proxyDataStream = ProxyDataStreamFactory.Create(systemField.FieldType.GenericTypeArguments[0]);
                Debug_EnsureNoDuplicateNodes(proxyDataStream);

                //Ensure the System's field is set to the data stream
                systemField.SetValue(instance, proxyDataStream);

                //Create the node
                TaskFlowNode node = CreateNode(proxyDataStream, taskSystem, taskDriver);

                //Update the node for any resolve channels
                IEnumerable<ResolveChannelAttribute> resolveChannelAttributes = systemField.GetCustomAttributes<ResolveChannelAttribute>();
                foreach (ResolveChannelAttribute resolveChannelAttribute in resolveChannelAttributes)
                {
                    node.RegisterAsResolveChannel(resolveChannelAttribute);
                }
            }
        }

        private TaskFlowNode CreateNode(AbstractProxyDataStream proxyDataStream, ITaskSystem taskSystem, ITaskDriver taskDriver)
        {
            TaskFlowNode node = new TaskFlowNode(this,
                                                 proxyDataStream,
                                                 taskSystem,
                                                 taskDriver);
            m_NodesLookupByDataStream.Add(proxyDataStream, node);
            if (taskDriver == null)
            {
                GetOrCreateNodeList(taskSystem, m_NodesLookupOwnedByTaskSystem).Add(node);
            }
            else
            {
                GetOrCreateNodeList(taskDriver, m_NodesLookupOwnedByTaskDriver).Add(node);
            }

            return node;
        }


        public JobConfig CreateJobConfig(ITaskSystem taskSystem,
                                                 ITaskDriver taskDriver,
                                                 AbstractProxyDataStream dataStream,
                                                 JobConfig.ScheduleJobDelegate scheduleJobFunction,
                                                 TaskFlowRoute route)
        {
            Debug_EnsureExists(dataStream);
            TaskFlowNode node = this[dataStream];

            JobConfig jobConfig = new JobConfig(this,
                                                taskSystem,
                                                taskDriver,
                                                scheduleJobFunction);

            node.RegisterJobConfig(route, jobConfig);

            return jobConfig;
        }

        public bool IsDataStreamRegistered(AbstractProxyDataStream dataStream)
        {
            return m_NodesLookupByDataStream.ContainsKey(dataStream);
        }

        public void DisposeFor(ITaskSystem system)
        {
            List<TaskFlowNode> nodes = GetOrCreateNodeList(system, m_NodesLookupOwnedByTaskSystem);
            DisposeNodes(nodes);

            List<ITaskDriver> taskDrivers = GetTaskDrivers(system);
            taskDrivers.Clear();
        }

        public void DisposeFor(ITaskDriver driver)
        {
            List<TaskFlowNode> nodes = GetOrCreateNodeList(driver, m_NodesLookupOwnedByTaskDriver);
            DisposeNodes(nodes);
        }

        public string GetDebugString(AbstractProxyDataStream dataStream)
        {
            Debug_EnsureExists(dataStream);
            return m_NodesLookupByDataStream[dataStream].ToString();
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


        public TaskFlowNode this[AbstractProxyDataStream dataStream]
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

        public BulkJobScheduler<AbstractProxyDataStream> CreateDataStreamBulkJobSchedulerFor(ITaskSystem system)
        {
            List<AbstractProxyDataStream> dataStreams = new List<AbstractProxyDataStream>();

            List<TaskFlowNode> nodes = m_NodesLookupOwnedByTaskSystem[system];
            foreach (TaskFlowNode node in nodes)
            {
                dataStreams.Add(node.DataStream);
            }

            return new BulkJobScheduler<AbstractProxyDataStream>(dataStreams);
        }

        public BulkJobScheduler<AbstractProxyDataStream> CreateDataStreamBulkJobSchedulerFor<TTaskDriver>(List<TTaskDriver> taskDrivers)
            where TTaskDriver : class, ITaskDriver
        {
            List<AbstractProxyDataStream> dataStreams = new List<AbstractProxyDataStream>();

            foreach (TTaskDriver driver in taskDrivers)
            {
                List<TaskFlowNode> nodes = m_NodesLookupOwnedByTaskDriver[driver];
                foreach (TaskFlowNode node in nodes)
                {
                    dataStreams.Add(node.DataStream);
                }
            }

            return new BulkJobScheduler<AbstractProxyDataStream>(dataStreams);
        }

        public Dictionary<TaskFlowRoute, BulkJobScheduler<JobConfig>> CreateJobConfigBulkJobSchedulerLookupFor(ITaskSystem system)
        {
            Dictionary<TaskFlowRoute, BulkJobScheduler<JobConfig>> lookup = new Dictionary<TaskFlowRoute, BulkJobScheduler<JobConfig>>();

            List<TaskFlowNode> nodes = m_NodesLookupOwnedByTaskSystem[system];
            foreach (TaskFlowRoute route in TASK_FLOW_ROUTE_VALUES)
            {
                lookup.Add(route, CreateJobConfigBulkJobScheduler(route, nodes));
            }

            return lookup;
        }

        public Dictionary<TaskFlowRoute, BulkJobScheduler<JobConfig>> CreateJobConfigBulkJobSchedulerLookupFor<TTaskDriver>(List<TTaskDriver> taskDrivers)
            where TTaskDriver : class, ITaskDriver
        {
            List<TaskFlowNode> nodes = new List<TaskFlowNode>();

            foreach (TTaskDriver driver in taskDrivers)
            {
                List<TaskFlowNode> taskDriverNodes = m_NodesLookupOwnedByTaskDriver[driver];
                nodes.AddRange(taskDriverNodes);
            }

            Dictionary<TaskFlowRoute, BulkJobScheduler<JobConfig>> lookup = new Dictionary<TaskFlowRoute, BulkJobScheduler<JobConfig>>();
            foreach (TaskFlowRoute route in TASK_FLOW_ROUTE_VALUES)
            {
                lookup.Add(route, CreateJobConfigBulkJobScheduler(route, nodes));
            }

            return lookup;
        }

        private BulkJobScheduler<JobConfig> CreateJobConfigBulkJobScheduler(TaskFlowRoute route, List<TaskFlowNode> nodes)
        {
            List<JobConfig> jobConfigs = new List<JobConfig>();
            foreach (TaskFlowNode node in nodes)
            {
                jobConfigs.AddRange(node.GetJobConfigsFor(route));
            }

            return new BulkJobScheduler<JobConfig>(jobConfigs);
        }

        public List<AbstractProxyDataStream> GetResolveChannelDataStreams<TResolveChannel>(TResolveChannel resolveChannel, ITaskSystem taskSystem, ITaskDriver taskDriver)
            where TResolveChannel : Enum
        {
            List<AbstractProxyDataStream> dataStreams = new List<AbstractProxyDataStream>();
            if (m_NodesLookupOwnedByTaskSystem.TryGetValue(taskSystem, out List<TaskFlowNode> nodes))
            {
                GetResolveChannelDataStreamsFromNodes(resolveChannel, nodes, dataStreams);
            }

            List<ITaskDriver> ownedTaskDrivers = GetTaskDrivers(taskSystem);
            foreach (ITaskDriver ownedTaskDriver in ownedTaskDrivers)
            {
                GetResolveChannelDataStreamsFromTaskDriver(resolveChannel, ownedTaskDriver, dataStreams);
            }

            GetResolveChannelDataStreamsFromTaskDriver(resolveChannel, taskDriver, dataStreams);

            return dataStreams;
        }

        private void GetResolveChannelDataStreamsFromTaskDriver<TResolveChannel>(TResolveChannel resolveChannel, ITaskDriver taskDriver, List<AbstractProxyDataStream> dataStreams)
            where TResolveChannel : Enum
        {
            if (taskDriver != null
             && m_NodesLookupOwnedByTaskDriver.TryGetValue(taskDriver, out List<TaskFlowNode> nodes))
            {
                GetResolveChannelDataStreamsFromNodes(resolveChannel, nodes, dataStreams);
            }
        }

        private void GetResolveChannelDataStreamsFromNodes<TResolveChannel>(TResolveChannel resolveChannel,
                                                                            List<TaskFlowNode> nodes,
                                                                            List<AbstractProxyDataStream> dataStreams)
            where TResolveChannel : Enum
        {
            foreach (TaskFlowNode node in nodes)
            {
                if (!node.IsResolveChannel(resolveChannel))
                {
                    continue;
                }

                dataStreams.Add(node.DataStream);
            }
        }

        private void RegisterTaskDriverToTaskSystem(ITaskSystem taskSystem, ITaskDriver taskDriver)
        {
            if (taskDriver == null)
            {
                return;
            }

            List<ITaskDriver> taskDrivers = GetTaskDrivers(taskSystem);
            taskDrivers.Add(taskDriver);
        }

        private List<ITaskDriver> GetTaskDrivers(ITaskSystem taskSystem)
        {
            if (!m_TaskDriversByTaskSystem.TryGetValue(taskSystem, out List<ITaskDriver> taskDrivers))
            {
                taskDrivers = new List<ITaskDriver>();
                m_TaskDriversByTaskSystem.Add(taskSystem, taskDrivers);
            }

            return taskDrivers;
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureExists(AbstractProxyDataStream dataStream)
        {
            if (!m_NodesLookupByDataStream.ContainsKey(dataStream))
            {
                throw new InvalidOperationException($"Trying to access a {nameof(TaskFlowNode)} with instance of {dataStream} but it doesn't exist!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNoDuplicateNodes(AbstractProxyDataStream dataStream)
        {
            if (m_NodesLookupByDataStream.ContainsKey(dataStream))
            {
                throw new InvalidOperationException($"Trying to create a new {nameof(TaskFlowNode)} with instance of {dataStream} but one already exists!");
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
        private void Debug_CheckFieldIsReadOnly(FieldInfo fieldInfo)
        {
            if (!fieldInfo.IsInitOnly)
            {
                throw new InvalidOperationException($"Field with name {fieldInfo.Name} on {fieldInfo.ReflectedType} is not marked as \"readonly\", please ensure that it is.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_CheckFieldTypeGenericTypeArguments(Type fieldType)
        {
            if (fieldType.GenericTypeArguments.Length != 1)
            {
                throw new InvalidOperationException($"Type {fieldType} is to be used to create a {typeof(ProxyDataStream<>)} but {fieldType} doesn't have the expected 1 generic type!");
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
