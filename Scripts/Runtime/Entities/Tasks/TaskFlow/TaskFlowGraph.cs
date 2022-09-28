using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Anvil.Unity.DOTS.Entities
{
    internal class TaskFlowGraph
    {
        private static readonly Type ABSTRACT_TASK_STREAM_TYPE = typeof(AbstractTaskStream);

        private readonly Dictionary<ITaskSystem, NodeLookup> m_DataNodesByTaskSystem;
        private readonly Dictionary<ITaskDriver, NodeLookup> m_DataNodesByTaskDriver;
        private readonly Dictionary<ITaskSystem, List<ITaskDriver>> m_TaskDriversByTaskSystem;
        private readonly Dictionary<ITaskSystem, JobNodeLookup> m_JobNodesByTaskSystem;
        private readonly Dictionary<ITaskDriver, JobNodeLookup> m_JobNodesByTaskDriver;

        public bool IsHardened
        {
            get;
            private set;
        }

        public TaskFlowGraph()
        {
            m_DataNodesByTaskSystem = new Dictionary<ITaskSystem, NodeLookup>();
            m_DataNodesByTaskDriver = new Dictionary<ITaskDriver, NodeLookup>();
            m_JobNodesByTaskSystem = new Dictionary<ITaskSystem, JobNodeLookup>();
            m_JobNodesByTaskDriver = new Dictionary<ITaskDriver, JobNodeLookup>();
            m_TaskDriversByTaskSystem = new Dictionary<ITaskSystem, List<ITaskDriver>>();
        }

        public void DisposeFor(ITaskSystem taskSystem)
        {
            NodeLookup nodeLookup = GetOrCreateNodeLookup(taskSystem, null);
            nodeLookup.Dispose();

            JobNodeLookup jobNodeLookup = GetOrCreateJobNodeLookup(taskSystem, null);
            jobNodeLookup.Dispose();

            List<ITaskDriver> taskDrivers = GetTaskDrivers(taskSystem);
            taskDrivers.Clear();
        }

        public void DisposeFor(ITaskSystem taskSystem, ITaskDriver taskDriver)
        {
            NodeLookup nodeLookup = GetOrCreateNodeLookup(taskSystem, taskDriver);
            nodeLookup.Dispose();

            JobNodeLookup jobNodeLookup = GetOrCreateJobNodeLookup(taskSystem, taskDriver);
            jobNodeLookup.Dispose();
        }


        //*************************************************************************************************************
        // DATA STREAMS
        //*************************************************************************************************************

        public void CreateTaskStreams(ITaskSystem taskSystem, ITaskDriver taskDriver = null)
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
                if (!ABSTRACT_TASK_STREAM_TYPE.IsAssignableFrom(systemField.FieldType))
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

                NodeLookup lookup = GetOrCreateNodeLookup(taskSystem, taskDriver);

                //Get the data type 
                AbstractTaskStream taskStream = TaskStreamFactory.Create(systemField.FieldType, systemField.FieldType.GenericTypeArguments[0]);

                //Ensure the System's field is set to the task stream
                systemField.SetValue(instance, taskStream);

                //Create the node
                DataStreamNode dataStreamNode = lookup.CreateDataStreamNode(taskStream, taskStream.GetDataStream());

                //Update the node for any resolve targets
                IEnumerable<ResolveTargetForAttribute> resolveTargetAttributes = systemField.GetCustomAttributes<ResolveTargetForAttribute>();
                foreach (ResolveTargetForAttribute resolveTargetAttribute in resolveTargetAttributes)
                {
                    dataStreamNode.RegisterAsResolveTarget(resolveTargetAttribute);
                }

                if (taskStream.IsCancellable)
                {
                    DataStreamNode pendingCancelDataStreamNode = lookup.CreateDataStreamNode(taskStream, taskStream.GetPendingCancelDataStream());
                    //No Resolve targets for this
                }
            }
        }

        public void RegisterCancelRequestsDataStream(CancelRequestsDataStream cancelRequestsDataStream, ITaskSystem taskSystem, ITaskDriver taskDriver)
        {
            NodeLookup lookup = GetOrCreateNodeLookup(taskSystem, taskDriver);
            CancelRequestsNode cancelRequestsNode = lookup.CreateCancelRequestsNode(cancelRequestsDataStream);
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

        public bool IsDataStreamRegistered(AbstractProxyDataStream dataStream, ITaskSystem taskSystem, ITaskDriver taskDriver)
        {
            NodeLookup systemNodeLookup = GetOrCreateNodeLookup(taskSystem, null);
            NodeLookup driverNodeLookup = GetOrCreateNodeLookup(taskSystem, taskDriver);
            return systemNodeLookup.IsDataStreamRegistered(dataStream) || driverNodeLookup.IsDataStreamRegistered(dataStream);
        }

        private NodeLookup GetOrCreateNodeLookup(ITaskSystem taskSystem, ITaskDriver taskDriver)
        {
            return (taskDriver == null)
                ? GetOrCreateNodeLookup(taskSystem, m_DataNodesByTaskSystem, taskSystem, null)
                : GetOrCreateNodeLookup(taskDriver, m_DataNodesByTaskDriver, taskSystem, taskDriver);
        }

        private NodeLookup GetOrCreateNodeLookup<TKey>(TKey key,
                                                       Dictionary<TKey, NodeLookup> dictionary,
                                                       ITaskSystem taskSystem,
                                                       ITaskDriver taskDriver)
        {
            if (!dictionary.TryGetValue(key, out NodeLookup lookup))
            {
                lookup = new NodeLookup(this, taskSystem, taskDriver);
                dictionary.Add(key, lookup);
            }

            return lookup;
        }

        public BulkJobScheduler<AbstractProxyDataStream> CreateDataStreamBulkJobSchedulerFor(ITaskSystem taskSystem)
        {
            List<AbstractProxyDataStream> dataStreams = new List<AbstractProxyDataStream>();

            NodeLookup lookup = GetOrCreateNodeLookup(taskSystem, null);
            lookup.PopulateWithDataStreams(dataStreams);

            return new BulkJobScheduler<AbstractProxyDataStream>(dataStreams);
        }

        public BulkJobScheduler<AbstractProxyDataStream> CreateDataStreamBulkJobSchedulerFor<TTaskDriver>(ITaskSystem taskSystem, List<TTaskDriver> taskDrivers)
            where TTaskDriver : class, ITaskDriver
        {
            List<AbstractProxyDataStream> dataStreams = new List<AbstractProxyDataStream>();

            foreach (TTaskDriver taskDriver in taskDrivers)
            {
                NodeLookup lookup = GetOrCreateNodeLookup(taskSystem, taskDriver);
                lookup.PopulateWithDataStreams(dataStreams);
            }

            return new BulkJobScheduler<AbstractProxyDataStream>(dataStreams);
        }

        public void PopulateJobResolveTargetMappingForTarget<TResolveTarget>(TResolveTarget resolveTarget, JobResolveTargetMapping jobResolveTargetMapping, ITaskSystem taskSystem)
            where TResolveTarget : Enum
        {
            //Get the Resolve Channels that exist on the system
            NodeLookup lookup = GetOrCreateNodeLookup(taskSystem, null);
            lookup.PopulateWithResolveTargetDataStreams(jobResolveTargetMapping, resolveTarget);

            //Get any Resolve Channels that exist on TaskDriver's owned by the system
            List<ITaskDriver> ownedTaskDrivers = GetTaskDrivers(taskSystem);
            foreach (ITaskDriver ownedTaskDriver in ownedTaskDrivers)
            {
                lookup = GetOrCreateNodeLookup(taskSystem, ownedTaskDriver);
                lookup.PopulateWithResolveTargetDataStreams(jobResolveTargetMapping, resolveTarget);
            }
        }

        public BulkJobScheduler<TaskDriverCancellationPropagator> CreateTaskDriversCancellationBulkJobSchedulerFor<TTaskDriver>(List<TTaskDriver> taskDrivers)
            where TTaskDriver : class, ITaskDriver
        {
            List<TaskDriverCancellationPropagator> propagators = new List<TaskDriverCancellationPropagator>();
            //For each task driver that exists, generate a propagator for it
            foreach (TTaskDriver taskDriver in taskDrivers)
            {
                ITaskSystem taskSystem = taskDriver.GetTaskSystem();
                NodeLookup lookup = GetOrCreateNodeLookup(taskSystem, taskDriver);
                TaskDriverCancellationPropagator propagator = lookup.CreateCancellationPropagator();
                propagators.Add(propagator);
            }

            return new BulkJobScheduler<TaskDriverCancellationPropagator>(propagators);
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        public AbstractJobConfig RegisterJobConfig(AbstractJobConfig jobConfig,
                                                   TaskFlowRoute route)
        {
            JobNodeLookup lookup = GetOrCreateJobNodeLookup(jobConfig.TaskSystem, jobConfig.TaskDriver);

            JobNode jobNode = lookup.CreateJobNode(route, jobConfig);

            return jobNode.JobConfig;
        }

        private JobNodeLookup GetOrCreateJobNodeLookup(ITaskSystem taskSystem, ITaskDriver taskDriver)
        {
            return (taskDriver == null)
                ? GetOrCreateJobNodeLookup(taskSystem, m_JobNodesByTaskSystem, taskSystem, null)
                : GetOrCreateJobNodeLookup(taskDriver, m_JobNodesByTaskDriver, taskSystem, taskDriver);
        }

        private JobNodeLookup GetOrCreateJobNodeLookup<TKey>(TKey key,
                                                             Dictionary<TKey, JobNodeLookup> dictionary,
                                                             ITaskSystem taskSystem,
                                                             ITaskDriver taskDriver)
        {
            if (!dictionary.TryGetValue(key, out JobNodeLookup lookup))
            {
                lookup = new JobNodeLookup(this, taskSystem, taskDriver);
                dictionary.Add(key, lookup);
            }

            return lookup;
        }

        public Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> CreateJobConfigBulkJobSchedulerLookupFor(ITaskSystem taskSystem)
        {
            Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> bulkSchedulers = new Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>>();
            Dictionary<TaskFlowRoute, List<AbstractJobConfig>> jobConfigs = new Dictionary<TaskFlowRoute, List<AbstractJobConfig>>();

            JobNodeLookup lookup = GetOrCreateJobNodeLookup(taskSystem, null);
            lookup.PopulateWithJobConfigs(jobConfigs);

            foreach (KeyValuePair<TaskFlowRoute, List<AbstractJobConfig>> entry in jobConfigs)
            {
                bulkSchedulers.Add(entry.Key, new BulkJobScheduler<AbstractJobConfig>(entry.Value));
            }

            return bulkSchedulers;
        }

        public Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> CreateJobConfigBulkJobSchedulerLookupFor<TTaskDriver>(ITaskSystem taskSystem, List<TTaskDriver> taskDrivers)
            where TTaskDriver : class, ITaskDriver
        {
            Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> bulkSchedulers = new Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>>();
            Dictionary<TaskFlowRoute, List<AbstractJobConfig>> jobConfigs = new Dictionary<TaskFlowRoute, List<AbstractJobConfig>>();

            foreach (TTaskDriver taskDriver in taskDrivers)
            {
                JobNodeLookup lookup = GetOrCreateJobNodeLookup(taskSystem, taskDriver);
                lookup.PopulateWithJobConfigs(jobConfigs);
            }

            foreach (KeyValuePair<TaskFlowRoute, List<AbstractJobConfig>> entry in jobConfigs)
            {
                bulkSchedulers.Add(entry.Key, new BulkJobScheduler<AbstractJobConfig>(entry.Value));
            }

            return bulkSchedulers;
        }


        //*************************************************************************************************************
        // FINALIZE
        //*************************************************************************************************************

        public void Harden()
        {
            if (IsHardened)
            {
                return;
            }

            IsHardened = true;

            foreach (JobNodeLookup jobNodeLookup in m_JobNodesByTaskSystem.Values)
            {
                jobNodeLookup.Harden();
            }

            foreach (JobNodeLookup jobNodeLookup in m_JobNodesByTaskDriver.Values)
            {
                jobNodeLookup.Harden();
            }

            //Iterate through all nodes registered to the graph to try and develop relationships. 
            //We'll end up getting islands of relationships between all the data so you can't necessarily have 
            //one entry and one exit.
            // foreach (DataStreamNode node in m_DataNodesLookupByDataStream.Values)
            // {
            //     //TODO: Implement
            //     // node.BuildConnections();
            // }

            Debug_EnsureJobFlowIsComplete();
        }

        //*************************************************************************************************************
        // UTILITY
        //*************************************************************************************************************

        public string GetDebugString(AbstractProxyDataStream dataStream, ITaskSystem taskSystem, ITaskDriver taskDriver)
        {
            NodeLookup lookup = GetOrCreateNodeLookup(taskSystem, taskDriver);
            return lookup[dataStream].ToString();
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNotHardened()
        {
            if (IsHardened)
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
