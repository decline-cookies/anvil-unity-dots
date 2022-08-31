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
    //TODO: DOCS
    public abstract class AbstractTaskDriver<TTaskDriverSystem, TKey, TTaskData, TResultDestinationType> : AbstractTaskDriver
        where TTaskDriverSystem : AbstractTaskDriverSystem<TKey, TTaskData>
        where TKey : unmanaged, IEquatable<TKey>
        where TTaskData : unmanaged, IKeyedData<TKey>, ITaskData
        where TResultDestinationType : Enum
    {
        private readonly VirtualDataLookup m_ResultsData;

        public VirtualData<TKey, TTaskData> TaskData
        {
            get => System.TaskData;
        }

        /// <summary>
        /// The <see cref="AbstractTaskDriverSystem"/> this TaskDriver belongs to.
        /// </summary>
        public new TTaskDriverSystem System
        {
            get => base.System as TTaskDriverSystem;
        }

        protected AbstractTaskDriver(World world) : base(world, world.GetOrCreateSystem<TTaskDriverSystem>())
        {
            m_ResultsData = new VirtualDataLookup();
        }

        protected override void DisposeSelf()
        {
            m_ResultsData.Dispose();
            base.DisposeSelf();
        }

        protected VirtualData<TKey, TTaskResultData> CreateResultsData<TTaskResultData>(TResultDestinationType resultDestinationType)
            where TTaskResultData : unmanaged, IKeyedData<TKey>
        {
            VirtualData<TKey, TTaskResultData> virtualData = VirtualData<TKey, TTaskResultData>.CreateAsResultsDestination(resultDestinationType, TaskData);
            m_ResultsData.AddData(virtualData);

            return virtualData;
        }

        protected sealed override JobHandle Consolidate(JobHandle dependsOn)
        {
            Debug_EnsureNoMainThreadWorkCurrentlyActive();
            JobHandle consolidateHandle = m_ResultsData.ConsolidateForFrame(dependsOn);
            return consolidateHandle;
        }
    }
    
    public abstract class AbstractTaskDriver : AbstractAnvilBase
    {
        internal static readonly BulkScheduleDelegate<AbstractTaskDriver> POPULATE_SCHEDULE_DELEGATE = BulkSchedulingUtil.CreateSchedulingDelegate<AbstractTaskDriver>(nameof(Populate), BindingFlags.Instance | BindingFlags.NonPublic);
        internal static readonly BulkScheduleDelegate<AbstractTaskDriver> UPDATE_SCHEDULE_DELEGATE = BulkSchedulingUtil.CreateSchedulingDelegate<AbstractTaskDriver>(nameof(Update), BindingFlags.Instance | BindingFlags.NonPublic);
        internal static readonly BulkScheduleDelegate<AbstractTaskDriver> CONSOLIDATE_SCHEDULE_DELEGATE = BulkSchedulingUtil.CreateSchedulingDelegate<AbstractTaskDriver>(nameof(Consolidate), BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<AbstractTaskDriver> m_SubTaskDrivers;
        private readonly List<JobTaskWorkConfig> m_PopulateJobData;
        private readonly List<JobTaskWorkConfig> m_UpdateJobData;

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

        protected AbstractTaskDriver(World world, AbstractTaskDriverSystem system)
        {
            World = world;
            System = system;
            m_SubTaskDrivers = new List<AbstractTaskDriver>();
            m_PopulateJobData = new List<JobTaskWorkConfig>();
            m_UpdateJobData = new List<JobTaskWorkConfig>();

            System.RegisterTaskDriver(this);
        }

        protected override void DisposeSelf()
        {
            foreach (AbstractTaskDriver childTaskDriver in m_SubTaskDrivers)
            {
                childTaskDriver.Dispose();
            }

            ReleaseMainThreadTaskWork();

            m_SubTaskDrivers.Clear();
            m_PopulateJobData.Clear();
            m_UpdateJobData.Clear();

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
            JobTaskWorkConfig config = new JobTaskWorkConfig(scheduleJobDelegate, System);
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
            JobTaskWorkConfig config = new JobTaskWorkConfig(scheduleJobDelegate, System);
            m_UpdateJobData.Add(config);
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

        protected abstract JobHandle Consolidate(JobHandle dependsOn);

        protected void RegisterSubTaskDriver(AbstractTaskDriver subTaskDriver)
        {
            m_SubTaskDrivers.Add(subTaskDriver);
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        protected void Debug_EnsureNoMainThreadWorkCurrentlyActive()
        {
            if (m_ActiveMainThreadTaskWorkConfig != null)
            {
                throw new InvalidOperationException($"Main Thread Task Work currently active, wait until after {nameof(ReleaseMainThreadTaskWork)} is called.");
            }
        }
    }
}
