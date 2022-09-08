using Anvil.CSharp.Data;
using Anvil.Unity.DOTS.Data;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A type of System that runs <see cref="AbstractTaskDriver"/>s during its update phase.
    /// </summary>
    public abstract partial class AbstractTaskSystem : AbstractAnvilSystemBase,
                                                       ITaskSystem
    {
        //TODO: Enable TaskDrivers again
        // private readonly List<TTaskDriver> m_TaskDrivers;
        private readonly ByteIDProvider m_TaskDriverIDProvider;

        private ISystemTask m_SystemTask;

        protected byte SystemLevelContext
        {
            get;
        }

        protected AbstractTaskSystem()
        {
            // m_TaskDrivers = new List<TTaskDriver>();

            m_TaskDriverIDProvider = new ByteIDProvider();
            SystemLevelContext = m_TaskDriverIDProvider.GetNextID();

            //TODO: Parse attributes to make the proper data
        }

        protected override void OnDestroy()
        {
            m_SystemTask?.Dispose();

            m_TaskDriverIDProvider.Dispose();

            //Note: We don't dispose TaskDrivers here because their parent or direct reference will do so. 
            // m_TaskDrivers.Clear();

            base.OnDestroy();
        }

        //TODO: #39 - Some way to remove the update Job

        protected SystemTask<TData> CreateTaskSystemStream<TData>()
            where TData : unmanaged, IProxyData
        {
            SystemTask<TData> systemTask = new SystemTask<TData>();
            //TODO: Expand this to support "many" with attributes, for now, just assuming one is fine
            m_SystemTask = systemTask;
            return systemTask;
        }

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
            // //Have drivers be given the chance to add to the Instance Data
            // dependsOn = m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver.POPULATE_SCHEDULE_DELEGATE);

            //Consolidate our instance data to operate on it
            dependsOn = m_SystemTask.ConsolidateForFrame(dependsOn);

            //TODO: #38 - Allow for cancels to occur

            //Allow the generic work to happen in the derived class
            dependsOn = m_SystemTask.PrepareAndSchedule(dependsOn);
            //TODO: Renable once we support "many" job configs
            // dependsOn = m_UpdateJobData.BulkScheduleParallel(dependsOn, JobTaskWorkConfig.PREPARE_AND_SCHEDULE_SCHEDULE_DELEGATE);

            // //Have drivers consolidate their result data
            // dependsOn = m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver.CONSOLIDATE_SCHEDULE_DELEGATE);

            //TODO: #38 - Allow for cancels on the drivers to occur

            // //Have drivers to do their own generic work
            // dependsOn = m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver.UPDATE_SCHEDULE_DELEGATE);

            //Ensure this system's dependency is written back
            return dependsOn;
        }

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
