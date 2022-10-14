using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class TaskFlowGraph
    {
        private static readonly TaskFlowRoute[] TASK_FLOW_ROUTE_VALUES = (TaskFlowRoute[])Enum.GetValues(typeof(TaskFlowRoute));
        
        private readonly HashSet<AbstractTaskSystem> m_TaskSystems;
        private readonly Dictionary<AbstractTaskSystem, NodeLookup> m_DataNodesByTaskSystem;
        private readonly Dictionary<AbstractTaskDriver, NodeLookup> m_DataNodesByTaskDriver;
        private readonly Dictionary<AbstractTaskSystem, JobNodeLookup> m_JobNodesByTaskSystem;
        private readonly Dictionary<AbstractTaskDriver, JobNodeLookup> m_JobNodesByTaskDriver;

        public bool IsHardened
        {
            get;
            private set;
        }

        public TaskFlowGraph()
        {
            m_TaskSystems = new HashSet<AbstractTaskSystem>();
            m_DataNodesByTaskSystem = new Dictionary<AbstractTaskSystem, NodeLookup>();
            m_DataNodesByTaskDriver = new Dictionary<AbstractTaskDriver, NodeLookup>();
            m_JobNodesByTaskSystem = new Dictionary<AbstractTaskSystem, JobNodeLookup>();
            m_JobNodesByTaskDriver = new Dictionary<AbstractTaskDriver, JobNodeLookup>();
        }
        
        //*************************************************************************************************************
        // REGISTRATION
        //*************************************************************************************************************
        
        public void RegisterTaskSystem(AbstractTaskSystem taskSystem)
        {
            m_TaskSystems.Add(taskSystem);
            RegisterCancelRequestsDataStream(taskSystem.CancelRequestsDataStream, taskSystem, null);
            RegisterTaskStreams(taskSystem.TaskStreams, taskSystem, null);
        }

        public void RegisterTaskDriver(AbstractTaskDriver taskDriver)
        {
            RegisterCancelRequestsDataStream(taskDriver.CancelRequestsDataStream, taskDriver.TaskSystem, taskDriver);
            RegisterTaskStreams(taskDriver.TaskStreams, taskDriver.TaskSystem, taskDriver);
        }

        //*************************************************************************************************************
        // DATA STREAMS
        //*************************************************************************************************************

        private void RegisterCancelRequestsDataStream(CancelRequestsDataStream cancelRequestsDataStream, AbstractTaskSystem taskSystem, AbstractTaskDriver taskDriver)
        {
            NodeLookup lookup = GetOrCreateNodeLookup(taskSystem, taskDriver);
            lookup.CreateCancelRequestsNode(cancelRequestsDataStream);
        }

        private void RegisterTaskStreams(List<AbstractTaskStream> taskStreams, AbstractTaskSystem taskSystem, AbstractTaskDriver taskDriver)
        {
            NodeLookup lookup = GetOrCreateNodeLookup(taskSystem, taskDriver);
            foreach (AbstractTaskStream taskStream in taskStreams)
            {
                lookup.CreateDataStreamNodes(taskStream);
            }
        }

        public bool IsDataStreamRegistered(AbstractEntityProxyDataStream dataStream, AbstractTaskSystem taskSystem, AbstractTaskDriver taskDriver)
        {
            NodeLookup systemNodeLookup = GetOrCreateNodeLookup(taskSystem, null);
            NodeLookup driverNodeLookup = GetOrCreateNodeLookup(taskSystem, taskDriver);
            return systemNodeLookup.IsDataStreamRegistered(dataStream) || driverNodeLookup.IsDataStreamRegistered(dataStream);
        }

        private NodeLookup GetOrCreateNodeLookup(AbstractTaskSystem taskSystem, AbstractTaskDriver taskDriver)
        {
            return (taskDriver == null)
                ? GetOrCreateNodeLookup(taskSystem, m_DataNodesByTaskSystem, taskSystem, null)
                : GetOrCreateNodeLookup(taskDriver, m_DataNodesByTaskDriver, taskSystem, taskDriver);
        }

        private NodeLookup GetOrCreateNodeLookup<TKey>(TKey key,
                                                       Dictionary<TKey, NodeLookup> dictionary,
                                                       AbstractTaskSystem taskSystem,
                                                       AbstractTaskDriver taskDriver)
        {
            if (!dictionary.TryGetValue(key, out NodeLookup lookup))
            {
                lookup = new NodeLookup(this, taskSystem, taskDriver);
                dictionary.Add(key, lookup);
            }

            return lookup;
        }

        public BulkJobScheduler<AbstractEntityProxyDataStream> CreateDataStreamBulkJobSchedulerFor(AbstractTaskSystem taskSystem)
        {
            List<AbstractEntityProxyDataStream> dataStreams = new List<AbstractEntityProxyDataStream>();

            NodeLookup lookup = GetOrCreateNodeLookup(taskSystem, null);
            lookup.AddDataStreamsTo(dataStreams);

            return new BulkJobScheduler<AbstractEntityProxyDataStream>(dataStreams.ToArray());
        }

        public BulkJobScheduler<AbstractEntityProxyDataStream> CreateDataStreamBulkJobSchedulerFor<TTaskDriver>(AbstractTaskSystem taskSystem, List<TTaskDriver> taskDrivers)
            where TTaskDriver : AbstractTaskDriver
        {
            List<AbstractEntityProxyDataStream> dataStreams = new List<AbstractEntityProxyDataStream>();

            foreach (TTaskDriver taskDriver in taskDrivers)
            {
                NodeLookup lookup = GetOrCreateNodeLookup(taskSystem, taskDriver);
                lookup.AddDataStreamsTo(dataStreams);
            }

            return new BulkJobScheduler<AbstractEntityProxyDataStream>(dataStreams.ToArray());
        }

        public void PopulateJobResolveTargetMappingForTarget<TResolveTargetType>(JobResolveTargetMapping jobResolveTargetMapping, AbstractTaskSystem taskSystem)
            where TResolveTargetType : unmanaged, IEntityProxyInstance
        {
            //Get the Resolve Channels that exist on the system
            NodeLookup lookup = GetOrCreateNodeLookup(taskSystem, null);
            lookup.AddResolveTargetDataStreamsTo<TResolveTargetType>(jobResolveTargetMapping);

            //Get any Resolve Channels that exist on TaskDriver's owned by the system
            foreach (AbstractTaskDriver ownedTaskDriver in taskSystem.TaskDrivers)
            {
                lookup = GetOrCreateNodeLookup(taskSystem, ownedTaskDriver);
                lookup.AddResolveTargetDataStreamsTo<TResolveTargetType>(jobResolveTargetMapping);
            }
        }

        public BulkJobScheduler<TaskDriverCancellationPropagator> CreateTaskDriversCancellationBulkJobSchedulerFor<TTaskDriver>(List<TTaskDriver> taskDrivers)
            where TTaskDriver : AbstractTaskDriver
        {
            List<TaskDriverCancellationPropagator> propagators = new List<TaskDriverCancellationPropagator>();
            //For each task driver that exists, generate a propagator for it
            foreach (TTaskDriver taskDriver in taskDrivers)
            {
                TaskDriverCancellationPropagator propagator = taskDriver.CancellationPropagator;
                propagators.Add(propagator);
            }

            return new BulkJobScheduler<TaskDriverCancellationPropagator>(propagators.ToArray());
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        public void RegisterJobConfig(AbstractJobConfig jobConfig,
                                      TaskFlowRoute route)
        {
            JobNodeLookup lookup = GetOrCreateJobNodeLookup(jobConfig.TaskSystem, jobConfig.TaskDriver);
            lookup.CreateJobNode(route, jobConfig);
        }

        private JobNodeLookup GetOrCreateJobNodeLookup(AbstractTaskSystem taskSystem, AbstractTaskDriver taskDriver)
        {
            return (taskDriver == null)
                ? GetOrCreateJobNodeLookup(taskSystem, m_JobNodesByTaskSystem, taskSystem, null)
                : GetOrCreateJobNodeLookup(taskDriver, m_JobNodesByTaskDriver, taskSystem, taskDriver);
        }

        private JobNodeLookup GetOrCreateJobNodeLookup<TKey>(TKey key,
                                                             Dictionary<TKey, JobNodeLookup> dictionary,
                                                             AbstractTaskSystem taskSystem,
                                                             AbstractTaskDriver taskDriver)
        {
            if (!dictionary.TryGetValue(key, out JobNodeLookup lookup))
            {
                lookup = new JobNodeLookup(this, taskSystem, taskDriver);
                dictionary.Add(key, lookup);
            }

            return lookup;
        }

        public Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> CreateJobConfigBulkJobSchedulerLookupFor(AbstractTaskSystem taskSystem)
        {
            Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> bulkSchedulers = new Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>>();
            Dictionary<TaskFlowRoute, List<AbstractJobConfig>> jobConfigs = CreateJobConfigsByRoute();

            JobNodeLookup lookup = GetOrCreateJobNodeLookup(taskSystem, null);
            foreach (TaskFlowRoute route in TASK_FLOW_ROUTE_VALUES)
            {
                lookup.AddJobConfigsTo(route, jobConfigs[route]);
            }

            foreach (KeyValuePair<TaskFlowRoute, List<AbstractJobConfig>> entry in jobConfigs)
            {
                bulkSchedulers.Add(entry.Key, new BulkJobScheduler<AbstractJobConfig>(entry.Value.ToArray()));
            }

            return bulkSchedulers;
        }

        public Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> CreateJobConfigBulkJobSchedulerLookupFor<TTaskDriver>(AbstractTaskSystem taskSystem, List<TTaskDriver> taskDrivers)
            where TTaskDriver : AbstractTaskDriver
        {
            Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> bulkSchedulers = new Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>>();
            Dictionary<TaskFlowRoute, List<AbstractJobConfig>> jobConfigs = CreateJobConfigsByRoute();

            foreach (TTaskDriver taskDriver in taskDrivers)
            {
                JobNodeLookup lookup = GetOrCreateJobNodeLookup(taskSystem, taskDriver);
                foreach (TaskFlowRoute route in TASK_FLOW_ROUTE_VALUES)
                {
                    lookup.AddJobConfigsTo(route, jobConfigs[route]);
                }
            }

            foreach (KeyValuePair<TaskFlowRoute, List<AbstractJobConfig>> entry in jobConfigs)
            {
                bulkSchedulers.Add(entry.Key, new BulkJobScheduler<AbstractJobConfig>(entry.Value.ToArray()));
            }

            return bulkSchedulers;
        }

        private Dictionary<TaskFlowRoute, List<AbstractJobConfig>> CreateJobConfigsByRoute()
        {
            Dictionary<TaskFlowRoute, List<AbstractJobConfig>> jobConfigs = new Dictionary<TaskFlowRoute, List<AbstractJobConfig>>();
            foreach (TaskFlowRoute route in TASK_FLOW_ROUTE_VALUES)
            {
                List<AbstractJobConfig> jobConfigList = new List<AbstractJobConfig>();
                jobConfigs.Add(route, jobConfigList);
            }
            return jobConfigs;
        }


        //*************************************************************************************************************
        // FINALIZE
        //*************************************************************************************************************

        public void ConfigureTaskSystemJobs()
        {
            foreach (AbstractTaskSystem taskSystem in m_TaskSystems)
            {
                taskSystem.ConfigureSystemJobs();
            }
        }
        
        public void Harden()
        {
            Debug_EnsureNotHardened();
            IsHardened = true;

            foreach (AbstractTaskSystem taskSystem in m_TaskSystems)
            {
                taskSystem.Harden();
            }

            //TODO: #66 - Build Relationships
            //Iterate through all nodes registered to the graph to try and develop relationships. 
            //We'll end up getting islands of relationships between all the data so you can't necessarily have 
            //one entry and one exit.
            // foreach (DataStreamNode node in m_DataNodesLookupByDataStream.Values)
            // {
            //     // node.BuildConnections();
            // }

            Debug_EnsureJobFlowIsComplete();
        }

        //*************************************************************************************************************
        // UTILITY
        //*************************************************************************************************************

        public string GetDebugString(AbstractEntityProxyDataStream dataStream, AbstractTaskSystem taskSystem, AbstractTaskDriver taskDriver)
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
                throw new InvalidOperationException($"{nameof(TaskFlowGraph)} is already hardened!");
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
                throw new InvalidOperationException($"Type {fieldType} is to be used to create a {typeof(EntityProxyDataStream<>)} but {fieldType} doesn't have the expected 1 generic type!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureJobFlowIsComplete()
        {
            //TODO: #67 - Ensure that all data is written to somehow and used so we don't have data loss.
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
            //         throw new InvalidOperationException($"{node.DataStream} located on {node.ToLocationString()}, does not have any job for {path}. {issue}");
            //     }
            // }
        }
    }
}
