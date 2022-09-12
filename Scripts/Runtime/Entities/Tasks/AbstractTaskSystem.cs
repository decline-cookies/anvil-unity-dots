using Anvil.CSharp.Data;
using System;
using System.Collections.Generic;
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
        //TODO: Enable TaskDrivers again, a Task System can only have one type of TaskDriver since it governs them
        // private readonly List<TTaskDriver> m_TaskDrivers;
        private readonly ByteIDProvider m_TaskDriverIDProvider;
        private readonly Dictionary<IProxyDataStream, ISystemTask> m_SystemTasks;


        protected byte SystemLevelContext
        {
            get;
        }

        protected AbstractTaskSystem()
        {
            // m_TaskDrivers = new List<TTaskDriver>();

            m_TaskDriverIDProvider = new ByteIDProvider();
            SystemLevelContext = m_TaskDriverIDProvider.GetNextID();

            m_SystemTasks = new Dictionary<IProxyDataStream, ISystemTask>();
            
            BuildSystemTasks();
            
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

        private void BuildSystemTasks()
        {
            Type systemType = GetType();
            //Get all the fields
            FieldInfo[] systemFields = systemType.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo systemField in systemFields)
            {
                //Figure out if they have been attributed with the SystemTaskAttribute
                SystemTaskAttribute systemTaskAttribute = systemField.GetCustomAttribute<SystemTaskAttribute>();
                if (systemTaskAttribute == null)
                {
                    continue;
                }
                //TODO: Ensure FieldType is ProxyDataStream
                
                //Get the data type 
                Type proxyDataType = systemField.FieldType.GenericTypeArguments[0];
                //Create the stream
                IProxyDataStream proxyDataStream = ProxyDataStreamFactory.Create(proxyDataType);
                //Create the task
                ISystemTask systemTask = SystemTaskFactory.Create(proxyDataType, proxyDataStream);
                
                //Ensure the System's field is set to the data stream
                systemField.SetValue(this, proxyDataStream);
                
                //Register the task
                m_SystemTasks.Add(proxyDataStream, systemTask);
            }
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
    }
}
