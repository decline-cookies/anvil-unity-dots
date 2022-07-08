using Anvil.CSharp.Command;
using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Task Drivers are the Data Oriented Design method of <see cref="AbstractCommand"/> in the Object Oriented Design
    /// Whereas a Command instance would be created to take an element of data through some specific logic steps
    /// to then output a result, a TaskDriver exists as a definition or route that all elements of data pass through to
    /// accomplish the same logical steps.
    /// Custom "Populate" jobs allow for getting elements of data into the TaskDriver.
    /// Custom "Update" jobs allow for stitching together sub task drivers input/output data.
    /// </summary>
    /// <typeparam name="TTaskDriverSystem">
    /// The <see cref="AbstractTaskDriverSystem"/> this TaskDriver belongs to. This ensures that the TaskDriver is
    /// executed during the System's update phase and that job dependencies are written to the correct system.
    /// </typeparam>
    public abstract class AbstractTaskDriver<TTaskDriverSystem> : AbstractTaskDriver
        where TTaskDriverSystem : AbstractTaskDriverSystem
    {
        /// <summary>
        /// The <see cref="AbstractTaskDriverSystem"/> this TaskDriver belongs to.
        /// </summary>
        public new TTaskDriverSystem System
        {
            get => base.System as TTaskDriverSystem;
        }

        protected AbstractTaskDriver(World world) : base(world, world.GetOrCreateSystem<TTaskDriverSystem>())
        {
        }
    }
    
    /// <inheritdoc cref="AbstractTaskDriver{TTaskDriverSystem}"/>
    public abstract class AbstractTaskDriver : AbstractAnvilBase
    {
        internal static readonly BulkScheduleDelegate<AbstractTaskDriver> POPULATE_SCHEDULE_DELEGATE = BulkSchedulingUtil.CreateSchedulingDelegate<BulkScheduleDelegate<AbstractTaskDriver>, AbstractTaskDriver>(nameof(Populate), BindingFlags.Instance | BindingFlags.NonPublic);
        internal static readonly BulkScheduleDelegate<AbstractTaskDriver> UPDATE_SCHEDULE_DELEGATE = BulkSchedulingUtil.CreateSchedulingDelegate<BulkScheduleDelegate<AbstractTaskDriver>, AbstractTaskDriver>(nameof(Update), BindingFlags.Instance | BindingFlags.NonPublic);
        internal static readonly BulkScheduleDelegate<AbstractTaskDriver> CANCEL_SCHEDULE_DELEGATE = BulkSchedulingUtil.CreateSchedulingDelegate<BulkScheduleDelegate<AbstractTaskDriver>, AbstractTaskDriver>(nameof(Cancel), BindingFlags.Instance | BindingFlags.NonPublic);
        internal static readonly BulkScheduleDelegate<AbstractTaskDriver> CONSOLIDATE_SCHEDULE_DELEGATE = BulkSchedulingUtil.CreateSchedulingDelegate<BulkScheduleDelegate<AbstractTaskDriver>, AbstractTaskDriver>(nameof(Consolidate), BindingFlags.Instance | BindingFlags.NonPublic);
        
        private readonly VirtualDataLookup m_InstanceDataLookup;

        private readonly List<AbstractTaskDriver> m_SubTaskDrivers;
        private readonly List<JobTaskWorkConfig> m_PopulateJobData;
        private readonly List<JobTaskWorkConfig> m_UpdateJobData;
        private readonly List<JobTaskWorkConfig> m_CancelJobData;

        private MainThreadTaskWorkConfig m_ActiveMainThreadTaskWorkConfig;

        /// <summary>
        /// The <see cref="World"/> this TaskDriver belongs to.
        /// </summary>
        public World World
        {
            get;
        }
        
        /// <inheritdoc cref="AbstractTaskDriver{TTaskDriverSystem}.System"/>
        public AbstractTaskDriverSystem System
        {
            get;
        }

        internal CancelVirtualData CancelData
        {
            get;
        }

        protected AbstractTaskDriver(World world, AbstractTaskDriverSystem system)
        {
            World = world;
            System = system;
            
            m_InstanceDataLookup = new VirtualDataLookup();
            CancelData = System.CancelData;

            m_SubTaskDrivers = new List<AbstractTaskDriver>();
            m_PopulateJobData = new List<JobTaskWorkConfig>();
            m_UpdateJobData = new List<JobTaskWorkConfig>();
            m_CancelJobData = new List<JobTaskWorkConfig>();

            System.RegisterTaskDriver(this);
        }

        protected override void DisposeSelf()
        {
            m_InstanceDataLookup.Dispose();


            foreach (AbstractTaskDriver childTaskDriver in m_SubTaskDrivers)
            {
                childTaskDriver.Dispose();
            }

            ReleaseMainThreadTaskWork();

            m_SubTaskDrivers.Clear();
            m_PopulateJobData.Clear();
            m_UpdateJobData.Clear();
            m_CancelJobData.Clear();

            base.DisposeSelf();
        }

        /// <summary>
        /// Configures this TaskDriver for performing work on the main thread immediately.
        /// <see cref="ReleaseMainThreadTaskWork"/> when finished. 
        /// </summary>
        /// <returns>A <see cref="MainThreadTaskWorkConfig"/> to chain on further customization.</returns>
        public MainThreadTaskWorkConfig CreateMainThreadTaskWork()
        {
            Debug_EnsureNoMainThreadWorkCurrentlyActive();
            m_ActiveMainThreadTaskWorkConfig = new MainThreadTaskWorkConfig(System);
            return m_ActiveMainThreadTaskWorkConfig;
        }
        
        /// <summary>
        /// Releases this TaskDriver from performing work on the main thread.
        /// <see cref="CreateMainThreadTaskWork"/>
        /// </summary>
        public void ReleaseMainThreadTaskWork()
        {
            m_ActiveMainThreadTaskWorkConfig?.Release();
            m_ActiveMainThreadTaskWorkConfig = null;
        }

        //TODO: #38 - Cancel plus cancel subtaskdrivers - public API
        //TODO: #39 - Some way to remove the populate job
        //TODO: #39 - Some way to remove the update Job

        /// <summary>
        /// Configures a job to be scheduled with the required data as specified by the <see cref="JobTaskWorkConfig"/>
        /// When scheduling, it will ensure that the data has the correct access.
        /// This job type is to be used to populate the TaskDriver so new elements can be processed.
        /// </summary>
        /// <param name="scheduleJobDelegate">The scheduling delegate to call</param>
        /// <returns>An instance of <see cref="JobTaskWorkConfig"/> to chain on further customization.</returns>
        public JobTaskWorkConfig ConfigurePopulateJob(JobTaskWorkConfig.ScheduleJobDelegate scheduleJobDelegate)
        {
            JobTaskWorkConfig config = new JobTaskWorkConfig(scheduleJobDelegate, System, false);
            m_PopulateJobData.Add(config);
            return config;
        }

        /// <summary>
        /// Configures a job to be scheduled with the required data as specified by the <see cref="JobTaskWorkConfig"/>
        /// When scheduling, it will ensure that the data has the correct access.
        /// This job type is to be used in the update phase of the TaskDriver so that results from a sub task driver
        /// can be stitched into the input for another.
        /// </summary>
        /// <param name="scheduleJobDelegate">The scheduling delegate to call</param>
        /// <returns>An instance of <see cref="JobTaskWorkConfig"/> to chain on further customization.</returns>
        protected JobTaskWorkConfig ConfigureUpdateJob(JobTaskWorkConfig.ScheduleJobDelegate scheduleJobDelegate)
        {
            JobTaskWorkConfig config = new JobTaskWorkConfig(scheduleJobDelegate, System, false);
            m_UpdateJobData.Add(config);
            return config;
        }
        
        /// <summary>
        /// Configures a job to be scheduled with the required data as specified by the <see cref="JobTaskWorkConfig"/>
        /// When scheduling, it will ensure that the data has the correct access.
        /// This job type is to be used in the cancel phase of the TaskDriver so that custom logic can happen when an
        /// element has been cancelled.
        /// </summary>
        /// <param name="scheduleJobDelegate">The scheduling delegate to call</param>
        /// <returns>An instance of <see cref="JobTaskWorkConfig"/> to chain on further customization.</returns>
        protected JobTaskWorkConfig ConfigureCancelJob(JobTaskWorkConfig.ScheduleJobDelegate scheduleJobDelegate)
        {
            JobTaskWorkConfig config = new JobTaskWorkConfig(scheduleJobDelegate, System, true);
            m_CancelJobData.Add(config);
            return config;
        }

        private JobHandle Populate(JobHandle dependsOn)
        {
            Debug_EnsureNoMainThreadWorkCurrentlyActive();
            return m_PopulateJobData.BulkScheduleParallel(dependsOn, JobTaskWorkConfig.PREPARE_AND_SCHEDULE_SCHEDULE_DELEGATE);
        }

        private JobHandle Update(JobHandle dependsOn)
        {
            Debug_EnsureNoMainThreadWorkCurrentlyActive();
            return m_UpdateJobData.BulkScheduleParallel(dependsOn, JobTaskWorkConfig.PREPARE_AND_SCHEDULE_SCHEDULE_DELEGATE);
        }

        private JobHandle Cancel(JobHandle dependsOn)
        {
            Debug_EnsureNoMainThreadWorkCurrentlyActive();
            return m_CancelJobData.BulkScheduleParallel(dependsOn, JobTaskWorkConfig.PREPARE_AND_SCHEDULE_SCHEDULE_DELEGATE);
        }

        private JobHandle Consolidate(JobHandle dependsOn)
        {
            Debug_EnsureNoMainThreadWorkCurrentlyActive();
            JobHandle cancelAccess = CancelData.AccessController.AcquireAsync(AccessType.SharedRead);
            JobHandle consolidateHandle = m_InstanceDataLookup.ConsolidateForFrame(JobHandle.CombineDependencies(dependsOn, cancelAccess), CancelData);
            CancelData.AccessController.ReleaseAsync(consolidateHandle);
            
            return consolidateHandle;
        }
        
        protected void RegisterSubTaskDriver(AbstractTaskDriver subTaskDriver)
        {
            m_SubTaskDrivers.Add(subTaskDriver);
        }

        protected VirtualData<TInstance> GetData<TInstance>()
            where TInstance : unmanaged, IKeyedData
        {
            return m_InstanceDataLookup.GetData<TInstance>();
        }

        protected VirtualData<TInstance> CreateData<TInstance>(params AbstractVirtualData[] sources)
            where TInstance : unmanaged, IKeyedData
        {
            VirtualData<TInstance> virtualData = VirtualData<TInstance>.Create(sources);
            m_InstanceDataLookup.AddData(virtualData);
            return virtualData;
        }


        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNoMainThreadWorkCurrentlyActive()
        {
            if (m_ActiveMainThreadTaskWorkConfig != null)
            {
                throw new InvalidOperationException($"Main Thread Task Work currently active, wait until after {nameof(ReleaseMainThreadTaskWork)} is called.");
            }
        }
    }
}
