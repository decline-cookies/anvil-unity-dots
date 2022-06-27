using Anvil.CSharp.Command;
using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            get;
        }

        protected AbstractTaskDriver(World world) : base(world)
        {
            System = World.GetOrCreateSystem<TTaskDriverSystem>();
            base.System = System;
            System.RegisterTaskDriver(this);

            ConstructData();
            ConstructChildTaskDrivers();
        }

        private void ConstructData()
        {
            InitData();
        }

        private void ConstructChildTaskDrivers()
        {
            InitSubTaskDriverRegistration();
        }
    }
    
    /// <inheritdoc cref="AbstractTaskDriver{TTaskDriverSystem}"/>
    public abstract class AbstractTaskDriver : AbstractAnvilBase
    {
        private readonly VirtualDataLookup m_InstanceData;
        private readonly List<AbstractTaskDriver> m_SubTaskDrivers;
        private readonly List<JobTaskWorkConfig> m_PopulateJobData;
        private readonly List<JobTaskWorkConfig> m_UpdateJobData;

        private readonly JobTaskWorkConfig.JobTaskWorkConfigBulkScheduler m_PopulateBulkScheduler;
        private readonly JobTaskWorkConfig.JobTaskWorkConfigBulkScheduler m_UpdateBulkScheduler;

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
            protected set;
        }

        protected AbstractTaskDriver(World world)
        {
            World = world;
            m_InstanceData = new VirtualDataLookup();
            m_SubTaskDrivers = new List<AbstractTaskDriver>();
            m_PopulateJobData = new List<JobTaskWorkConfig>();
            m_UpdateJobData = new List<JobTaskWorkConfig>();

            m_PopulateBulkScheduler = new JobTaskWorkConfig.JobTaskWorkConfigBulkScheduler(m_PopulateJobData);
            m_UpdateBulkScheduler = new JobTaskWorkConfig.JobTaskWorkConfigBulkScheduler(m_UpdateJobData);
        }

        protected override void DisposeSelf()
        {
            m_InstanceData.Dispose();

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
        /// Hook to allow for creating new <see cref="VirtualData{TKey,TInstance}"/>
        /// via <see cref="CreateData{TKey,TInstance}"/>
        /// </summary>
        protected abstract void InitData();
        /// <summary>
        /// Hook to allow for registering new <see cref="AbstractTaskDriver"/> via
        /// <see cref="RegisterSubTaskDriver"/>
        /// </summary>
        protected abstract void InitSubTaskDriverRegistration();
        
        /// <summary>
        /// Configures this TaskDriver for performing work on the main thread immediately.
        /// <see cref="ReleaseMainThreadTaskWork"/> when finished. 
        /// </summary>
        /// <returns>A <see cref="MainThreadTaskWorkConfig"/> to chain on further customization.</returns>
        public MainThreadTaskWorkConfig ConfigureMainThreadTaskWork()
        {
            DebugEnsureNoMainThreadWorkCurrentlyActive();
            m_ActiveMainThreadTaskWorkConfig = new MainThreadTaskWorkConfig(System);
            return m_ActiveMainThreadTaskWorkConfig;
        }
        
        /// <summary>
        /// Releases this TaskDriver from performing work on the main thread.
        /// <see cref="ConfigureMainThreadTaskWork"/>
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
        /// <param name="jobDataDelegate">The scheduling delegate to call</param>
        /// <returns>An instance of <see cref="JobTaskWorkConfig"/> to chain on further customization.</returns>
        public JobTaskWorkConfig ConfigurePopulateJob(JobTaskWorkConfig.JobDataDelegate jobDataDelegate)
        {
            JobTaskWorkConfig config = new JobTaskWorkConfig(jobDataDelegate, System);
            m_PopulateJobData.Add(config);
            return config;
        }

        /// <summary>
        /// Configures a job to be scheduled with the required data as specified by the <see cref="JobTaskWorkConfig"/>
        /// When scheduling, it will ensure that the data has the correct access.
        /// This job type is to be used in the update phase of the TaskDriver so that results from a sub task driver
        /// can be stitched into the input for another.
        /// </summary>
        /// <param name="jobDataDelegate">The scheduling delegate to call</param>
        /// <returns>An instance of <see cref="JobTaskWorkConfig"/> to chain on further customization.</returns>
        protected JobTaskWorkConfig ConfigureUpdateJob(JobTaskWorkConfig.JobDataDelegate jobDataDelegate)
        {
            JobTaskWorkConfig config = new JobTaskWorkConfig(jobDataDelegate, System);
            m_UpdateJobData.Add(config);
            return config;
        }

        private JobHandle Populate(JobHandle dependsOn)
        {
            DebugEnsureNoMainThreadWorkCurrentlyActive();
            return m_PopulateBulkScheduler.BulkSchedule(dependsOn);
        }

        private JobHandle Update(JobHandle dependsOn)
        {
            DebugEnsureNoMainThreadWorkCurrentlyActive();
            return m_UpdateBulkScheduler.BulkSchedule(dependsOn);
        }

        private JobHandle Consolidate(JobHandle dependsOn)
        {
            DebugEnsureNoMainThreadWorkCurrentlyActive();
            JobHandle consolidateHandle = m_InstanceData.ConsolidateForFrame(dependsOn);
            return consolidateHandle;
        }
        
        protected void RegisterSubTaskDriver(AbstractTaskDriver subTaskDriver)
        {
            m_SubTaskDrivers.Add(subTaskDriver);
        }
        
        protected VirtualData<TKey, TInstance> GetData<TKey, TInstance>()
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            return m_InstanceData.GetData<TKey, TInstance>();
        }

        protected VirtualData<TKey, TInstance> CreateData<TKey, TInstance>(params AbstractVirtualData[] sources)
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            VirtualData<TKey, TInstance> virtualData = VirtualData<TKey, TInstance>.Create(sources);
            m_InstanceData.AddData(virtualData);
            return virtualData;
        }
        
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void DebugEnsureNoMainThreadWorkCurrentlyActive()
        {
            if (m_ActiveMainThreadTaskWorkConfig != null)
            {
                throw new InvalidOperationException($"Main Thread Task Work currently active, wait until after {nameof(ReleaseMainThreadTaskWork)} is called.");
            }
        }
        
        //*************************************************************************************************************
        // BULK SCHEDULERS
        //*************************************************************************************************************
        
        internal class TaskDriverPopulateBulkScheduler : AbstractBulkScheduler<AbstractTaskDriver>
        {
            public TaskDriverPopulateBulkScheduler(List<AbstractTaskDriver> list) : base(list)
            {
            }

            protected override JobHandle ScheduleItem(AbstractTaskDriver item, JobHandle dependsOn)
            {
                return item.Populate(dependsOn);
            }
        }

        internal class TaskDriverUpdateBulkScheduler : AbstractBulkScheduler<AbstractTaskDriver>
        {
            public TaskDriverUpdateBulkScheduler(List<AbstractTaskDriver> list) : base(list)
            {
            }

            protected override JobHandle ScheduleItem(AbstractTaskDriver item, JobHandle dependsOn)
            {
                return item.Update(dependsOn);
            }
        }

        internal class TaskDriverConsolidateBulkScheduler : AbstractBulkScheduler<AbstractTaskDriver>
        {
            public TaskDriverConsolidateBulkScheduler(List<AbstractTaskDriver> list) : base(list)
            {
            }

            protected override JobHandle ScheduleItem(AbstractTaskDriver item, JobHandle dependsOn)
            {
                return item.Consolidate(dependsOn);
            }
        }
    }
}
