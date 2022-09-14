using Anvil.CSharp.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A type of System that runs <see cref="AbstractTaskDriver"/>s during its update phase.
    /// </summary>
    public abstract partial class AbstractTaskSystem<TTaskDriver, TTaskSystem> : AbstractTaskSystem,
                                                                                 ITaskSystem
        where TTaskDriver : AbstractTaskDriver<TTaskDriver, TTaskSystem>
        where TTaskSystem : AbstractTaskSystem<TTaskDriver, TTaskSystem>
    {
        private readonly List<TTaskDriver> m_TaskDrivers;
        private readonly ByteIDProvider m_TaskDriverIDProvider;

        private readonly Dictionary<IProxyDataStream, DataFlowNode> m_DataFlowNodes;
        private readonly Dictionary<DataFlowNode.DataFlowPath, List<IJobConfig>> m_JobConfigsLookup;
        private readonly Dictionary<DataFlowNode.DataFlowPath, NativeArray<JobHandle>> m_JobDependenciesLookup;

        private readonly List<IProxyDataStream> m_AllDataStreams;
        private NativeArray<JobHandle> m_AllDataStreamsDependencies;

        private readonly byte m_SystemLevelContext;
        private bool m_IsCreatePhaseComplete;

        protected AbstractTaskSystem()
        {
            m_TaskDrivers = new List<TTaskDriver>();

            m_TaskDriverIDProvider = new ByteIDProvider();
            m_SystemLevelContext = m_TaskDriverIDProvider.GetNextID();

            m_DataFlowNodes = new Dictionary<IProxyDataStream, DataFlowNode>();

            m_JobConfigsLookup = new Dictionary<DataFlowNode.DataFlowPath, List<IJobConfig>>();
            foreach (DataFlowNode.DataFlowPath path in DataFlowNode.FlowPathValues)
            {
                m_JobConfigsLookup.Add(path, new List<IJobConfig>());
            }

            m_JobDependenciesLookup = new Dictionary<DataFlowNode.DataFlowPath, NativeArray<JobHandle>>();

            m_AllDataStreams = new List<IProxyDataStream>();

            CreateProxyDataStreams();

            //TODO: 3. Custom Update Job Types
            //TODO: Create the custom Update Job so we can parse to the different result channels.
        }

        protected override void OnDestroy()
        {
            //Dispose any data that we own
            foreach (DataFlowNode node in m_DataFlowNodes.Values)
            {
                if (node.DataOwner == DataFlowNode.Owner.Driver)
                {
                    continue;
                }

                node.Dispose();
            }

            m_DataFlowNodes.Clear();
            m_JobConfigsLookup.Clear();


            m_TaskDriverIDProvider.Dispose();

            foreach (NativeArray<JobHandle> processorDependencies in m_JobDependenciesLookup.Values)
            {
                if (processorDependencies.IsCreated)
                {
                    processorDependencies.Dispose();
                }
            }

            m_JobDependenciesLookup.Clear();

            if (m_AllDataStreamsDependencies.IsCreated)
            {
                m_AllDataStreamsDependencies.Dispose();
            }

            //Note: We don't dispose TaskDrivers here because their parent or direct reference will do so. 
            m_TaskDrivers.Clear();

            base.OnDestroy();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            if (m_IsCreatePhaseComplete)
            {
                return;
            }
            m_IsCreatePhaseComplete = true;
            
            Debug_CheckDataStreamJobsExist();
            BuildOptimizedCollections();
        }

        private void CreateProxyDataStreams()
        {
            List<IProxyDataStream> dataStreams = TaskDataStreamUtil.GenerateProxyDataStreamsOnType(this);
            foreach (IProxyDataStream dataStream in dataStreams)
            {
                CreateDataFlowNode(dataStream, null);
            }
        }

        //TODO: #39 - Some way to remove the update Job

        internal byte RegisterTaskDriver(TTaskDriver taskDriver)
        {
            Debug_EnsureTaskDriverSystemRelationship(taskDriver);
            m_TaskDrivers.Add(taskDriver);

            return m_TaskDriverIDProvider.GetNextID();
        }

        internal void RegisterTaskDriverDataStream(IProxyDataStream dataStream, ITaskDriver taskDriver)
        {
            CreateDataFlowNode(dataStream, taskDriver);
        }

        private void CreateDataFlowNode(IProxyDataStream dataStream, ITaskDriver taskDriver)
        {
            Debug_EnsureNoDuplicateDataFlowNodes(dataStream);
            DataFlowNode node = new DataFlowNode(dataStream, this, taskDriver);
            m_DataFlowNodes.Add(dataStream, node);
            m_AllDataStreams.Add(dataStream);
        }

        protected override void OnUpdate()
        {
            Dependency = UpdateTaskDriverSystem(Dependency);
        }

        //TODO: Determine if we need custom configs for job types
        protected IJobConfig ConfigureUpdateJobFor<TInstance>(ProxyDataStream<TInstance> dataStream,
                                                              JobConfig<TInstance>.ScheduleJobDelegate scheduleJobFunction,
                                                              BatchStrategy batchStrategy)
            where TInstance : unmanaged, IProxyInstance
        {
            return ConfigureJobFor(dataStream,
                                   scheduleJobFunction,
                                   batchStrategy,
                                   DataFlowNode.DataFlowPath.Update);
        }

        //TODO: Determine if we need custom configs for job types
        internal IJobConfig ConfigurePopulateJobFor<TInstance>(ProxyDataStream<TInstance> dataStream,
                                                               JobConfig<TInstance>.ScheduleJobDelegate scheduleJobFunction,
                                                               BatchStrategy batchStrategy,
                                                               TTaskDriver taskDriver)
            where TInstance : unmanaged, IProxyInstance
        {
            return ConfigureJobFor(dataStream,
                                   scheduleJobFunction,
                                   batchStrategy,
                                   DataFlowNode.DataFlowPath.Populate);
        }


        private IJobConfig ConfigureJobFor<TInstance>(ProxyDataStream<TInstance> dataStream,
                                                      JobConfig<TInstance>.ScheduleJobDelegate scheduleJobFunction,
                                                      BatchStrategy batchStrategy,
                                                      DataFlowNode.DataFlowPath path)
            where TInstance : unmanaged, IProxyInstance
        {
            //TODO: Double check error messages make sense depending if this came from a TaskDriver or the System
            Debug_EnsureDataStreamIntegrity(dataStream, typeof(TInstance));

            DataFlowNode node = m_DataFlowNodes[dataStream];
            
            Debug_EnsurePriorToCreatePhaseComplete(node, path);

            JobConfig<TInstance> jobConfig = new JobConfig<TInstance>(World,
                                                                      node.TaskDriver?.Context ?? m_SystemLevelContext,
                                                                      scheduleJobFunction,
                                                                      batchStrategy,
                                                                      dataStream);

            node.AddJobConfig(path, jobConfig);

            return jobConfig;
        }


        private void BuildOptimizedCollections()
        {
            foreach (DataFlowNode node in m_DataFlowNodes.Values)
            {
                foreach (DataFlowNode.DataFlowPath path in DataFlowNode.FlowPathValues)
                {
                    m_JobConfigsLookup[path].AddRange(node.GetJobConfigsFor(path));
                }
            }

            foreach (DataFlowNode.DataFlowPath path in DataFlowNode.FlowPathValues)
            {
                m_JobDependenciesLookup.Add(path, new NativeArray<JobHandle>(m_JobConfigsLookup[path].Count, Allocator.Persistent));
            }

            m_AllDataStreamsDependencies = new NativeArray<JobHandle>(m_AllDataStreams.Count, Allocator.Persistent);
        }

        private JobHandle UpdateTaskDriverSystem(JobHandle dependsOn)
        {
            //Have drivers be given the chance to write to system data
            dependsOn = ScheduleJobs(dependsOn, DataFlowNode.DataFlowPath.Populate);


            //Consolidate all data streams associated to prepare to operate on them
            //TODO: Should we only consolidate system level data?
            dependsOn = m_AllDataStreams.BulkScheduleParallel(dependsOn,
                                                              ref m_AllDataStreamsDependencies,
                                                              IProxyDataStream.CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION);

            //TODO: #38 - Allow for cancels to occur

            //Allow the update jobs to occur on our owned data streams
            //TODO: Only on System owned data or also Task Drivers?
            dependsOn = ScheduleJobs(dependsOn, DataFlowNode.DataFlowPath.Update);

            //TODO: Renable once Task Drivers are enabled
            // //Have drivers consolidate their result data
            // dependsOn = m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver.CONSOLIDATE_SCHEDULE_DELEGATE);

            //TODO: #38 - Allow for cancels on the drivers to occur

            //TODO: Renable once Task Drivers are enabled
            // //Have drivers to do their own generic work
            // dependsOn = m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver.UPDATE_SCHEDULE_DELEGATE);

            //Ensure this system's dependency is written back
            return dependsOn;
        }

        private JobHandle ScheduleJobs(JobHandle dependsOn, DataFlowNode.DataFlowPath path)
        {
            List<IJobConfig> jobs = m_JobConfigsLookup[path];
            NativeArray<JobHandle> dependencies = m_JobDependenciesLookup[path];
            return jobs.BulkScheduleParallel(dependsOn,
                                             ref dependencies,
                                             IJobConfig.PREPARE_AND_SCHEDULE_FUNCTION);
        }


        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureTaskDriverSystemRelationship(TTaskDriver taskDriver)
        {
            if (taskDriver.TaskSystem != this)
            {
                throw new InvalidOperationException($"{taskDriver} is part of system {taskDriver.TaskSystem} but it should be {this}!");
            }

            if (m_TaskDrivers.Contains(taskDriver))
            {
                throw new InvalidOperationException($"Trying to add {taskDriver} to {this}'s list of Task Drivers but it is already there!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_CheckDataStreamJobsExist()
        {
            foreach (DataFlowNode node in m_DataFlowNodes.Values)
            {
                foreach (DataFlowNode.DataFlowPath path in DataFlowNode.FlowPathValues)
                {
                    if (node.HasJobsFor(path))
                    {
                        continue;
                    }

                    string issue = path switch
                    {
                        DataFlowNode.DataFlowPath.Populate => $"The data will never be populated with any instances.",
                        DataFlowNode.DataFlowPath.Update   => $"There will be data loss as this data will never be updated to flow to a results location or continue in the stream.",
                        _                                  => throw new ArgumentOutOfRangeException($"Tried to generate issue string for {path} but no code path satisfies!")
                    };
                    
                    throw new InvalidOperationException($"{node.DataStream.GetType()} located on {node.ToLocationString()}, does not have any job for {path}. {issue}");
                }
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureDataStreamIntegrity(IProxyDataStream dataStream, Type expectedType)
        {
            if (dataStream == null)
            {
                throw new InvalidOperationException($"Data Stream is null! Possible causes: "
                                                  + $"\n1. The incorrect reference to a {typeof(ProxyDataStream<>).Name}<{expectedType.Name}> was passed in such as referencing a hidden variable or something not defined on this class. {typeof(ProxyDataStream<>)}'s are created via reflection in the constructor of this class."
                                                  + $"\n2. The {nameof(ConfigureJobFor)} function wasn't called from {nameof(OnCreate)}. The reflection to create {typeof(ProxyDataStream<>).Name}<{expectedType.Name}>'s hasn't happened yet.");
            }

            if (!m_DataFlowNodes.ContainsKey(dataStream))
            {
                throw new InvalidOperationException($"DataStream of {dataStream.GetType().Name} was not registered to this class. Was it defined as a part of this class or TaskDrivers associated with this class?");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsurePriorToCreatePhaseComplete(DataFlowNode node, DataFlowNode.DataFlowPath path)
        {
            if (m_IsCreatePhaseComplete)
            {
                throw new InvalidOperationException($"Trying to create a {path} job on {node.ToLocationString()} but the create phase for systems is complete! Please ensure that you configure your jobs in the {nameof(OnCreate)} or earlier.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNoDuplicateDataFlowNodes(IProxyDataStream dataStream)
        {
            if (m_DataFlowNodes.ContainsKey(dataStream))
            {
                throw new InvalidOperationException($"Trying to register the same instance of {dataStream.GetType()} on {GetType()} but one already exists!");
            }
        }
    }

    //TODO: Might be able to get rid of this
    public abstract class AbstractTaskSystem : AbstractAnvilSystemBase
    {
    }
}
