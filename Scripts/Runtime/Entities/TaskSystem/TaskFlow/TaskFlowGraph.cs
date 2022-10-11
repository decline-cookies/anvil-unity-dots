using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class TaskFlowGraph
    {
        private static readonly Type ABSTRACT_TASK_STREAM_TYPE = typeof(AbstractTaskStream);

        private readonly Dictionary<AbstractTaskSystem, NodeLookup> m_DataNodesByTaskSystem;
        private readonly Dictionary<AbstractTaskDriver, NodeLookup> m_DataNodesByTaskDriver;
        private readonly Dictionary<AbstractTaskSystem, List<AbstractTaskDriver>> m_TaskDriversByTaskSystem;
        private readonly Dictionary<AbstractTaskSystem, JobNodeLookup> m_JobNodesByTaskSystem;
        private readonly Dictionary<AbstractTaskDriver, JobNodeLookup> m_JobNodesByTaskDriver;

        public bool IsHardened
        {
            get;
            private set;
        }

        public TaskFlowGraph()
        {
            m_DataNodesByTaskSystem = new Dictionary<AbstractTaskSystem, NodeLookup>();
            m_DataNodesByTaskDriver = new Dictionary<AbstractTaskDriver, NodeLookup>();
            m_JobNodesByTaskSystem = new Dictionary<AbstractTaskSystem, JobNodeLookup>();
            m_JobNodesByTaskDriver = new Dictionary<AbstractTaskDriver, JobNodeLookup>();
            m_TaskDriversByTaskSystem = new Dictionary<AbstractTaskSystem, List<AbstractTaskDriver>>();
        }

        public void DisposeFor(AbstractTaskSystem taskSystem)
        {
            NodeLookup nodeLookup = GetOrCreateNodeLookup(taskSystem, null);
            nodeLookup.Dispose();

            JobNodeLookup jobNodeLookup = GetOrCreateJobNodeLookup(taskSystem, null);
            jobNodeLookup.Dispose();

            List<AbstractTaskDriver> taskDrivers = GetTaskDrivers(taskSystem);
            taskDrivers.Clear();
        }

        public void DisposeFor(AbstractTaskSystem taskSystem, AbstractTaskDriver taskDriver)
        {
            NodeLookup nodeLookup = GetOrCreateNodeLookup(taskSystem, taskDriver);
            nodeLookup.Dispose();

            JobNodeLookup jobNodeLookup = GetOrCreateJobNodeLookup(taskSystem, taskDriver);
            jobNodeLookup.Dispose();
        }


        //*************************************************************************************************************
        // DATA STREAMS
        //*************************************************************************************************************

        public void CreateTaskStreams(AbstractTaskSystem taskSystem, AbstractTaskDriver taskDriver = null)
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

                IgnoreTaskStreamAttribute ignoreTaskStreamAttribute = systemField.GetCustomAttribute<IgnoreTaskStreamAttribute>();
                if (ignoreTaskStreamAttribute != null)
                {
                    continue;
                }

                Debug_CheckFieldIsReadOnly(systemField);
                Debug_CheckFieldTypeGenericTypeArguments(systemField.FieldType);

                NodeLookup lookup = GetOrCreateNodeLookup(taskSystem, taskDriver);

                //Get the data type 
                Type entityProxyInstanceType = systemField.FieldType.GenericTypeArguments[0];
                AbstractTaskStream taskStream = TaskStreamFactory.Create(systemField.FieldType, entityProxyInstanceType);

                //Ensure the System's field is set to the task stream
                systemField.SetValue(instance, taskStream);

                //TODO: Rework this a bit in #68, #71 or #63. A bit weird to pass in the TaskStream and then also the DataStream explicitly when it could be pulled of. 
                //TODO: Needs to jive with the cancelled version as well.
                //Create the node
                DataStreamNode dataStreamNode = lookup.CreateDataStreamNode(taskStream,
                                                                            taskStream.GetDataStream(),
                                                                            systemField.GetCustomAttribute<ResolveTargetAttribute>() != null);


                if (taskStream.IsCancellable)
                {
                    DataStreamNode pendingCancelDataStreamNode = lookup.CreateDataStreamNode(taskStream, taskStream.GetPendingCancelDataStream(), false);
                    //No Resolve targets for this
                }
            }
        }

        public void RegisterCancelRequestsDataStream(CancelRequestsDataStream cancelRequestsDataStream, AbstractTaskSystem taskSystem, AbstractTaskDriver taskDriver)
        {
            NodeLookup lookup = GetOrCreateNodeLookup(taskSystem, taskDriver);
            CancelRequestsNode cancelRequestsNode = lookup.CreateCancelRequestsNode(cancelRequestsDataStream);
        }

        private void RegisterTaskDriverToTaskSystem(AbstractTaskSystem taskSystem, AbstractTaskDriver taskDriver)
        {
            if (taskDriver == null)
            {
                return;
            }

            List<AbstractTaskDriver> taskDrivers = GetTaskDrivers(taskSystem);
            taskDrivers.Add(taskDriver);
        }

        private List<AbstractTaskDriver> GetTaskDrivers(AbstractTaskSystem taskSystem)
        {
            if (!m_TaskDriversByTaskSystem.TryGetValue(taskSystem, out List<AbstractTaskDriver> taskDrivers))
            {
                taskDrivers = new List<AbstractTaskDriver>();
                m_TaskDriversByTaskSystem.Add(taskSystem, taskDrivers);
            }

            return taskDrivers;
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
            List<AbstractTaskDriver> ownedTaskDrivers = GetTaskDrivers(taskSystem);
            foreach (AbstractTaskDriver ownedTaskDriver in ownedTaskDrivers)
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
                NodeLookup lookup = GetOrCreateNodeLookup(taskDriver.TaskSystem, taskDriver);
                TaskDriverCancellationPropagator propagator = lookup.CreateCancellationPropagator();
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

            JobNode jobNode = lookup.CreateJobNode(route, jobConfig);
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
            Dictionary<TaskFlowRoute, List<AbstractJobConfig>> jobConfigs = new Dictionary<TaskFlowRoute, List<AbstractJobConfig>>();

            JobNodeLookup lookup = GetOrCreateJobNodeLookup(taskSystem, null);
            lookup.AddJobConfigsTo(jobConfigs);

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
            Dictionary<TaskFlowRoute, List<AbstractJobConfig>> jobConfigs = new Dictionary<TaskFlowRoute, List<AbstractJobConfig>>();

            foreach (TTaskDriver taskDriver in taskDrivers)
            {
                JobNodeLookup lookup = GetOrCreateJobNodeLookup(taskSystem, taskDriver);
                lookup.AddJobConfigsTo(jobConfigs);
            }

            foreach (KeyValuePair<TaskFlowRoute, List<AbstractJobConfig>> entry in jobConfigs)
            {
                bulkSchedulers.Add(entry.Key, new BulkJobScheduler<AbstractJobConfig>(entry.Value.ToArray()));
            }

            return bulkSchedulers;
        }


        //*************************************************************************************************************
        // FINALIZE
        //*************************************************************************************************************

        public void Harden()
        {
            Debug_EnsureNotHardened();
            IsHardened = true;

            foreach (JobNodeLookup jobNodeLookup in m_JobNodesByTaskSystem.Values)
            {
                jobNodeLookup.Harden();
            }

            foreach (JobNodeLookup jobNodeLookup in m_JobNodesByTaskDriver.Values)
            {
                jobNodeLookup.Harden();
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
            //         throw new InvalidOperationException($"{node.DataStream.GetType()} located on {node.ToLocationString()}, does not have any job for {path}. {issue}");
            //     }
            // }
        }
    }
}
