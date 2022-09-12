using Anvil.CSharp.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A type of System that runs <see cref="AbstractTaskDriver"/>s during its update phase.
    /// </summary>
    public abstract partial class AbstractTaskSystem : AbstractAnvilSystemBase,
                                                       ITaskSystem
    {
        private static readonly Type I_PROXY_DATA_TYPE = typeof(IProxyData);
        private static readonly Type I_PROXY_DATA_STREAM_TYPE = typeof(IProxyDataStream);

        //TODO: Enable TaskDrivers again, a Task System can only have one type of TaskDriver since it governs them
        // private readonly List<TTaskDriver> m_TaskDrivers;
        private readonly ByteIDProvider m_TaskDriverIDProvider;
        private readonly Dictionary<IProxyDataStream, ISystemTask> m_SystemTasks;
        private readonly byte m_SystemLevelContext;
        private bool m_HasCheckedDataStreamUpdateJobsIntegrity;

        protected AbstractTaskSystem()
        {
            // m_TaskDrivers = new List<TTaskDriver>();

            m_TaskDriverIDProvider = new ByteIDProvider();
            m_SystemLevelContext = m_TaskDriverIDProvider.GetNextID();

            m_SystemTasks = new Dictionary<IProxyDataStream, ISystemTask>();

            BuildProxyDataStreams();

            //TODO: 2. Enable TaskDrivers
            //TODO: Task Drivers will hook into the Systems and run their own Tasks to populate.
            //TODO: 3. Custom Update Job Types
            //TODO: Create the custom Update Job so we can parse to the different result channels.
        }

        protected override void OnDestroy()
        {
            foreach (ISystemTask systemTask in m_SystemTasks.Values)
            {
                systemTask.Dispose();
            }

            m_SystemTasks.Clear();

            m_TaskDriverIDProvider.Dispose();

            //Note: We don't dispose TaskDrivers here because their parent or direct reference will do so. 
            // m_TaskDrivers.Clear();

            base.OnDestroy();
        }
        
        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            Debug_CheckDataStreamUpdateJobsExist();
        }

        private void BuildProxyDataStreams()
        {
            Type systemType = GetType();
            //Get all the fields
            FieldInfo[] systemFields = systemType.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo systemField in systemFields)
            {
                if (!I_PROXY_DATA_STREAM_TYPE.IsAssignableFrom(systemField.FieldType))
                {
                    continue;
                }

                //TODO: Check if the opt-out attribute is there
                Debug_CheckFieldTypeGenericTypeArguments(systemField.FieldType);

                //Get the data type 
                IProxyDataStream proxyDataStream = CreateProxyDataStream(systemField.FieldType.GenericTypeArguments[0]);

                //Ensure the System's field is set to the data stream
                systemField.SetValue(this, proxyDataStream);
            }

            //TODO: Allow for properties instead of just fields or get private fields and then check if it's a property to get opt out attribute
        }

        private IProxyDataStream CreateProxyDataStream(Type proxyDataType)
        {
            //Create the stream
            IProxyDataStream proxyDataStream = ProxyDataStreamFactory.Create(proxyDataType);

            //Add the stream to the lookup
            //Null initially because we want a record of data that doesn't yet have the jobs associated
            m_SystemTasks.Add(proxyDataStream, null);

            return proxyDataStream;
        }

        //TODO: #39 - Some way to remove the update Job

        //TODO: RE-ENABLE IF NEEDED
        // internal byte RegisterTaskDriver(TTaskDriver taskDriver)
        // {
        //     Debug_EnsureTaskDriverSystemRelationship(taskDriver);
        //     m_TaskDrivers.Add(taskDriver);
        //     
        //     return m_TaskDriverIDProvider.GetNextID();
        // }

        protected override void OnUpdate()
        {
            //TODO: Safety check on ensuring we have Job Config for each Data
            Dependency = UpdateTaskDriverSystem(Dependency);
        }

        protected IUpdateJobConfig ConfigureUpdateJobFor<TData>(ProxyDataStream<TData> dataStream, UpdateJobConfig<TData>.ScheduleJobDelegate scheduleJobFunction, BatchStrategy batchStrategy)
            where TData : unmanaged, IProxyData
        {
            Debug_EnsureDataStreamIntegrity(dataStream);
            Debug_EnsureNoUpdateJobExists(dataStream);
            
            UpdateJobConfig<TData> updateJobConfig = new UpdateJobConfig<TData>(World, m_SystemLevelContext, scheduleJobFunction, batchStrategy, dataStream);
            SystemTask<TData> systemTask = new SystemTask<TData>(dataStream, updateJobConfig);
            m_SystemTasks[dataStream] = systemTask;
            return updateJobConfig;
        }

        private JobHandle UpdateTaskDriverSystem(JobHandle dependsOn)
        {
            //TODO: Renable once Task Drivers are enabled
            // //Have drivers be given the chance to add to the Instance Data
            // dependsOn = m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver.POPULATE_SCHEDULE_DELEGATE);

            //Consolidate our instance data to operate on it
            // dependsOn = m_SystemTask.ConsolidateForFrame(dependsOn);

            //TODO: #38 - Allow for cancels to occur

            //Allow the generic work to happen in the derived class
            // dependsOn = m_SystemTask.PrepareAndSchedule(dependsOn);
            //TODO: Renable once we support "many" job configs
            // dependsOn = m_UpdateJobData.BulkScheduleParallel(dependsOn, JobTaskWorkConfig.PREPARE_AND_SCHEDULE_SCHEDULE_DELEGATE);

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

        //TODO: RENABLE IF NEEDED
        // [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        // private void Debug_EnsureTaskDriverSystemRelationship(TTaskDriver taskDriver)
        // {
        //     if (taskDriver.System != this)
        //     {
        //         throw new InvalidOperationException($"{taskDriver} is part of system {taskDriver.System} but it should be {this}!");
        //     }
        //
        //     if (m_TaskDrivers.Contains(taskDriver))
        //     {
        //         throw new InvalidOperationException($"Trying to add {taskDriver} to {this}'s list of Task Drivers but it is already there!");
        //     }
        // }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_CheckFieldTypeGenericTypeArguments(Type fieldType)
        {
            if (fieldType.GenericTypeArguments.Length != 1)
            {
                throw new InvalidOperationException($"Type {fieldType} is to be used to create a {typeof(ProxyDataStream<>)} but {fieldType} doesn't have the expected 1 generic type!");
            }

            Type genericType = fieldType.GenericTypeArguments[0];
            if (!I_PROXY_DATA_TYPE.IsAssignableFrom(genericType))
            {
                throw new InvalidOperationException($"Type {genericType} does not implement {I_PROXY_DATA_TYPE}!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_CheckDataStreamUpdateJobsExist()
        {
            if (m_HasCheckedDataStreamUpdateJobsIntegrity)
            {
                return;
            }
            m_HasCheckedDataStreamUpdateJobsIntegrity = true;

            foreach (KeyValuePair<IProxyDataStream, ISystemTask> entry in m_SystemTasks)
            {
                if (entry.Value == null)
                {
                    throw new InvalidOperationException($"Data Stream of {entry.Key} exists but {nameof(ConfigureUpdateJobFor)} wasn't called in {nameof(OnCreate)}! Please ensure all {typeof(ProxyDataStream<>)} have a corresponding job configured via {nameof(ConfigureUpdateJobFor)} to ensure there is no data loss.");
                }
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureDataStreamIntegrity(IProxyDataStream dataStream)
        {
            if (dataStream == null)
            {
                throw new InvalidOperationException($"Data Stream is null! Possible causes: "
                                                  + $"\n1. The incorrect reference to a {typeof(ProxyDataStream<>)} was passed in such as referencing a hidden variable or something not defined on this class. {typeof(ProxyDataStream<>)}'s are created via reflection in the constructor of this class."
                                                  + $"\n2. The {nameof(ConfigureUpdateJobFor)} function wasn't called from {nameof(OnCreate)}. The reflection to create {typeof(ProxyDataStream<>)}'s hasn't happened yet.");
            }

            if (!m_SystemTasks.ContainsKey(dataStream))
            {
                throw new InvalidOperationException($"DataStream of {dataStream.GetType()} was not registered to this class. Was it defined as a part of this class?");
            }
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNoUpdateJobExists(IProxyDataStream dataStream)
        {
            ISystemTask systemTask = m_SystemTasks[dataStream];
            if (systemTask != null)
            {
                throw new InvalidOperationException($"DataStream of {dataStream.GetType()} already has an Update Job configured!");
            }
        }
    }
}
