using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class TaskFlowGraph
    {
        private static readonly TaskFlowRoute[] TASK_FLOW_ROUTE_VALUES = (TaskFlowRoute[])Enum.GetValues(typeof(TaskFlowRoute));
        
        private readonly HashSet<AbstractTaskDriverSystem> m_TaskSystems;
        private readonly HashSet<AbstractTaskDriver> m_TaskDrivers;
        private readonly List<AbstractTaskDriver> m_TopLevelTaskDrivers;
        private readonly Dictionary<AbstractTaskDriverSystem, NodeLookup> m_DataNodesByTaskSystem;
        private readonly Dictionary<AbstractTaskDriver, NodeLookup> m_DataNodesByTaskDriver;
        private readonly Dictionary<AbstractTaskDriverSystem, JobNodeLookup> m_JobNodesByTaskSystem;
        private readonly Dictionary<AbstractTaskDriver, JobNodeLookup> m_JobNodesByTaskDriver;

        public bool IsHardened
        {
            get;
            private set;
        }

        public TaskFlowGraph()
        {
            m_TaskSystems = new HashSet<AbstractTaskDriverSystem>();
            m_TaskDrivers = new HashSet<AbstractTaskDriver>();
            m_TopLevelTaskDrivers = new List<AbstractTaskDriver>();
            m_DataNodesByTaskSystem = new Dictionary<AbstractTaskDriverSystem, NodeLookup>();
            m_DataNodesByTaskDriver = new Dictionary<AbstractTaskDriver, NodeLookup>();
            m_JobNodesByTaskSystem = new Dictionary<AbstractTaskDriverSystem, JobNodeLookup>();
            m_JobNodesByTaskDriver = new Dictionary<AbstractTaskDriver, JobNodeLookup>();
        }
        
        //*************************************************************************************************************
        // REGISTRATION
        //*************************************************************************************************************
        
        public void RegisterTaskSystem(AbstractTaskDriverSystem taskSystem)
        {
            m_TaskSystems.Add(taskSystem);
            RegisterDataStreams(taskSystem.TaskData, taskSystem, null);
        }

        public void RegisterTaskDriver(AbstractTaskDriver taskDriver)
        {
            m_TaskDrivers.Add(taskDriver);
            if (taskDriver.Parent == null)
            {
                m_TopLevelTaskDrivers.Add(taskDriver);
            }
            RegisterDataStreams(taskDriver.TaskData, taskDriver.GoverningTaskSystem, taskDriver);
        }

        //*************************************************************************************************************
        // DATA STREAMS
        //*************************************************************************************************************

        private void RegisterDataStreams(TaskData taskData, AbstractTaskDriverSystem taskSystem, AbstractTaskDriver taskDriver)
        {
            NodeLookup lookup = GetOrCreateNodeLookup(taskSystem, taskDriver);
            foreach (AbstractDataStream dataStream in taskData.AllPublicDataStreams)
            {
                lookup.CreateDataStreamNodes(dataStream);
            }
        }

        public bool IsDataStreamRegistered(AbstractDataStream dataStream, AbstractTaskDriverSystem taskSystem, AbstractTaskDriver taskDriver)
        {
            NodeLookup systemNodeLookup = GetOrCreateNodeLookup(taskSystem, null);
            NodeLookup driverNodeLookup = GetOrCreateNodeLookup(taskSystem, taskDriver);
            return systemNodeLookup.IsDataStreamRegistered(dataStream) || driverNodeLookup.IsDataStreamRegistered(dataStream);
        }

        private NodeLookup GetOrCreateNodeLookup(AbstractTaskDriverSystem taskSystem, AbstractTaskDriver taskDriver)
        {
            return (taskDriver == null)
                ? GetOrCreateNodeLookup(taskSystem, m_DataNodesByTaskSystem, taskSystem, null)
                : GetOrCreateNodeLookup(taskDriver, m_DataNodesByTaskDriver, taskSystem, taskDriver);
        }

        private NodeLookup GetOrCreateNodeLookup<TKey>(TKey key,
                                                       Dictionary<TKey, NodeLookup> dictionary,
                                                       AbstractTaskDriverSystem taskSystem,
                                                       AbstractTaskDriver taskDriver)
        {
            if (!dictionary.TryGetValue(key, out NodeLookup lookup))
            {
                lookup = new NodeLookup(this, taskSystem, taskDriver);
                dictionary.Add(key, lookup);
            }

            return lookup;
        }

        public void AddAllDataStreamsTo(List<AbstractDataStream> abstractDataStreams)
        {
            //TODO: #108 or #66, #67, #68 Move this to the TaskData
            foreach (AbstractTaskDriverSystem taskSystem in m_TaskSystems)
            {
                abstractDataStreams.AddRange(taskSystem.TaskData.DataStreams);
                abstractDataStreams.AddRange(taskSystem.TaskData.CancellableDataStreams);
                abstractDataStreams.AddRange(taskSystem.TaskData.CancelResultDataStreams);
                foreach (AbstractDataStream abstractDataStream in taskSystem.TaskData.CancellableDataStreams)
                {
                    IUntypedCancellableDataStream cancellableDataStream = (IUntypedCancellableDataStream)abstractDataStream;
                    abstractDataStreams.Add(cancellableDataStream.UntypedPendingCancelDataStream);
                }
                abstractDataStreams.Add(taskSystem.TaskData.CancelCompleteDataStream);
                abstractDataStreams.Add(taskSystem.TaskData.CancelRequestDataStream);

                foreach (AbstractTaskDriver taskDriver in taskSystem.TaskDrivers)
                {
                    abstractDataStreams.AddRange(taskDriver.TaskData.DataStreams);
                    abstractDataStreams.AddRange(taskDriver.TaskData.CancellableDataStreams);
                    abstractDataStreams.AddRange(taskDriver.TaskData.CancelResultDataStreams);
                    foreach (AbstractDataStream abstractDataStream in taskDriver.TaskData.CancellableDataStreams)
                    {
                        IUntypedCancellableDataStream cancellableDataStream = (IUntypedCancellableDataStream)abstractDataStream;
                        abstractDataStreams.Add(cancellableDataStream.UntypedPendingCancelDataStream);
                    }
                    abstractDataStreams.Add(taskDriver.TaskData.CancelCompleteDataStream);
                    abstractDataStreams.Add(taskDriver.TaskData.CancelRequestDataStream);
                }
            }
        }

        public BulkJobScheduler<AbstractDataStream> CreateWorldDataStreamBulkJobScheduler()
        {
            List<AbstractDataStream> dataStreams = new List<AbstractDataStream>();

            //TODO: #66, #67 or #68 Can make this nicer
            foreach (AbstractTaskDriverSystem taskSystem in m_TaskSystems)
            {
                dataStreams.AddRange(taskSystem.TaskData.AllPublicDataStreams);

                foreach (AbstractTaskDriver taskDriver in taskSystem.TaskDrivers)
                {
                    dataStreams.AddRange(taskDriver.TaskData.AllPublicDataStreams);
                }
            }

            return new BulkJobScheduler<AbstractDataStream>(dataStreams.ToArray());
        }

        public BulkJobScheduler<CancelRequestDataStream> CreateWorldCancelRequestsDataStreamBulkJobScheduler()
        {
            List<CancelRequestDataStream> cancelRequests = new List<CancelRequestDataStream>();
            //TODO: #66, #67 or #68 Can make this nicer
            foreach (AbstractTaskDriverSystem taskSystem in m_TaskSystems)
            {
                cancelRequests.Add(taskSystem.TaskData.CancelRequestDataStream);

                foreach (AbstractTaskDriver taskDriver in taskSystem.TaskDrivers)
                {
                    cancelRequests.Add(taskDriver.TaskData.CancelRequestDataStream);
                }
            }

            return new BulkJobScheduler<CancelRequestDataStream>(cancelRequests.ToArray());
        }

        public BulkJobScheduler<AbstractDataStream> CreateWorldCancelCompleteBulkJobScheduler()
        {
            List<CancelCompleteDataStream> cancelCompletes = new List<CancelCompleteDataStream>();
            //TODO: #66, #67 or #68 Can make this nicer
            foreach (AbstractTaskDriverSystem taskSystem in m_TaskSystems)
            {
                cancelCompletes.Add(taskSystem.TaskData.CancelCompleteDataStream);
                foreach (AbstractTaskDriver taskDriver in taskSystem.TaskDrivers)
                {
                    cancelCompletes.Add(taskDriver.TaskData.CancelCompleteDataStream);
                }
            }

            return new BulkJobScheduler<AbstractDataStream>(cancelCompletes.ToArray());
        }

        public BulkJobScheduler<AbstractDataStream> CreateWorldPendingCancelBulkJobScheduler()
        {
            List<AbstractDataStream> dataStreams = new List<AbstractDataStream>();

            //TODO: #66, #67 or #68 Can make this nicer
            foreach (AbstractTaskDriverSystem taskSystem in m_TaskSystems)
            {
                foreach (AbstractDataStream dataStream in taskSystem.TaskData.CancellableDataStreams)
                {
                    IUntypedCancellableDataStream cancellableDataStream = (IUntypedCancellableDataStream)dataStream;
                    dataStreams.Add(cancellableDataStream.UntypedPendingCancelDataStream);
                }

                foreach (AbstractTaskDriver taskDriver in taskSystem.TaskDrivers)
                {
                    foreach (AbstractDataStream dataStream in taskDriver.TaskData.CancellableDataStreams)
                    {
                        IUntypedCancellableDataStream cancellableDataStream = (IUntypedCancellableDataStream)dataStream;
                        dataStreams.Add(cancellableDataStream.UntypedPendingCancelDataStream);
                    }
                }
            }
            return new BulkJobScheduler<AbstractDataStream>(dataStreams.ToArray());
        }

        public BulkJobScheduler<TaskDriverCancelFlow> CreateWorldCancelFlowBulkJobScheduler()
        {
            List<TaskDriverCancelFlow> topLevelCancelFlows = new List<TaskDriverCancelFlow>();
            foreach (AbstractTaskDriver taskDriver in m_TopLevelTaskDrivers)
            {
                topLevelCancelFlows.Add(taskDriver.CancelFlow);
            }

            return new BulkJobScheduler<TaskDriverCancelFlow>(topLevelCancelFlows.ToArray());
        }


        public void PopulateJobResolveTargetMappingForTarget<TResolveTargetType>(JobResolveTargetMapping jobResolveTargetMapping, CommonTaskSet commonTaskSet)
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

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        public void RegisterJobConfig(AbstractJobConfig jobConfig,
                                      TaskFlowRoute route)
        {
            JobNodeLookup lookup = GetOrCreateJobNodeLookup(jobConfig.TaskSystem, jobConfig.TaskDriver);
            lookup.CreateJobNode(route, jobConfig);
        }

        private JobNodeLookup GetOrCreateJobNodeLookup(AbstractTaskDriverSystem taskSystem, AbstractTaskDriver taskDriver)
        {
            return (taskDriver == null)
                ? GetOrCreateJobNodeLookup(taskSystem, m_JobNodesByTaskSystem, taskSystem, null)
                : GetOrCreateJobNodeLookup(taskDriver, m_JobNodesByTaskDriver, taskSystem, taskDriver);
        }

        private JobNodeLookup GetOrCreateJobNodeLookup<TKey>(TKey key,
                                                             Dictionary<TKey, JobNodeLookup> dictionary,
                                                             AbstractTaskDriverSystem taskSystem,
                                                             AbstractTaskDriver taskDriver)
        {
            if (!dictionary.TryGetValue(key, out JobNodeLookup lookup))
            {
                lookup = new JobNodeLookup(this, taskSystem, taskDriver);
                dictionary.Add(key, lookup);
            }

            return lookup;
        }

        public Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> CreateJobConfigBulkJobSchedulerLookupFor(AbstractTaskDriverSystem taskSystem)
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

        public Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> CreateJobConfigBulkJobSchedulerLookupFor<TTaskDriver>(AbstractTaskDriverSystem taskSystem, List<TTaskDriver> taskDrivers)
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
            foreach (AbstractTaskDriverSystem taskSystem in m_TaskSystems)
            {
                taskSystem.ConfigureSystemJobs();
            }
        }
        
        public void Harden()
        {
            Debug_EnsureNotHardened();
            IsHardened = true;

            foreach (AbstractTaskDriverSystem taskSystem in m_TaskSystems)
            {
                taskSystem.Harden();
            }

            foreach (AbstractTaskDriver taskDriver in m_TopLevelTaskDrivers)
            {
                taskDriver.CancelFlow.BuildScheduling();
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

        public string GetDebugString(AbstractDataStream dataStream, AbstractTaskDriverSystem taskSystem, AbstractTaskDriver taskDriver)
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
                throw new InvalidOperationException($"Type {fieldType} is to be used to create a {typeof(AbstractTypedDataStream<>)} but {fieldType} doesn't have the expected 1 generic type!");
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
