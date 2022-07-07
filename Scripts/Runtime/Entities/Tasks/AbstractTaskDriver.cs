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
    public abstract class AbstractTaskDriver<TKey, TTaskDriverSystem> : AbstractTaskDriver<TKey>
        where TKey : unmanaged, IEquatable<TKey>
        where TTaskDriverSystem : AbstractTaskDriverSystem<TKey>
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
    public abstract class AbstractTaskDriver<TKey> : AbstractAnvilBase
        where TKey : unmanaged, IEquatable<TKey>
    {
        internal static readonly BulkScheduleDelegate<AbstractTaskDriver<TKey>> POPULATE_SCHEDULE_DELEGATE = BulkSchedulingUtil.CreateSchedulingDelegate<BulkScheduleDelegate<AbstractTaskDriver<TKey>>, AbstractTaskDriver<TKey>>(nameof(Populate), BindingFlags.Instance | BindingFlags.NonPublic);
        internal static readonly BulkScheduleDelegate<AbstractTaskDriver<TKey>> UPDATE_SCHEDULE_DELEGATE = BulkSchedulingUtil.CreateSchedulingDelegate<BulkScheduleDelegate<AbstractTaskDriver<TKey>>, AbstractTaskDriver<TKey>>(nameof(Update), BindingFlags.Instance | BindingFlags.NonPublic);
        internal static readonly BulkScheduleDelegate<AbstractTaskDriver<TKey>> CANCEL_SCHEDULE_DELEGATE = BulkSchedulingUtil.CreateSchedulingDelegate<BulkScheduleDelegate<AbstractTaskDriver<TKey>>, AbstractTaskDriver<TKey>>(nameof(Cancel), BindingFlags.Instance | BindingFlags.NonPublic);
        internal static readonly BulkScheduleDelegate<AbstractTaskDriver<TKey>> CONSOLIDATE_SCHEDULE_DELEGATE = BulkSchedulingUtil.CreateSchedulingDelegate<BulkScheduleDelegate<AbstractTaskDriver<TKey>>, AbstractTaskDriver<TKey>>(nameof(Consolidate), BindingFlags.Instance | BindingFlags.NonPublic);
        
        private readonly VirtualDataLookup<TKey> m_InstanceDataLookup;

        private readonly List<AbstractTaskDriver<TKey>> m_SubTaskDrivers;
        private readonly List<JobTaskWorkConfig<TKey>> m_PopulateJobData;
        private readonly List<JobTaskWorkConfig<TKey>> m_UpdateJobData;
        private readonly List<JobTaskWorkConfig<TKey>> m_CancelJobData;

        private MainThreadTaskWorkConfig<TKey> m_ActiveMainThreadTaskWorkConfig;
        private AbstractTaskDriver<TKey> m_Parent;

        /// <summary>
        /// The <see cref="World"/> this TaskDriver belongs to.
        /// </summary>
        public World World
        {
            get;
        }
        
        /// <inheritdoc cref="AbstractTaskDriver{TTaskDriverSystem}.System"/>
        public AbstractTaskDriverSystem<TKey> System
        {
            get;
        }

        internal CancelVirtualData<TKey> CancelData
        {
            get;
            private set;
        }

        protected AbstractTaskDriver(World world, AbstractTaskDriverSystem<TKey> system)
        {
            World = world;
            System = system;
            
            m_InstanceDataLookup = new VirtualDataLookup<TKey>();
            CancelData = new CancelVirtualData<TKey>();
            
            m_SubTaskDrivers = new List<AbstractTaskDriver<TKey>>();
            m_PopulateJobData = new List<JobTaskWorkConfig<TKey>>();
            m_UpdateJobData = new List<JobTaskWorkConfig<TKey>>();
            m_CancelJobData = new List<JobTaskWorkConfig<TKey>>();

            System.RegisterTaskDriver(this);
        }

        protected override void DisposeSelf()
        {
            m_InstanceDataLookup.Dispose();
            //If we don't have a parent, then we own the CancelData
            if (m_Parent == null)
            {
                CancelData.Dispose();
            }

            m_Parent = null;
            

            foreach (AbstractTaskDriver<TKey> childTaskDriver in m_SubTaskDrivers)
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
        public MainThreadTaskWorkConfig<TKey> CreateMainThreadTaskWork()
        {
            Debug_EnsureNoMainThreadWorkCurrentlyActive();
            m_ActiveMainThreadTaskWorkConfig = new MainThreadTaskWorkConfig<TKey>(System);
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
        public JobTaskWorkConfig<TKey> ConfigurePopulateJob(JobTaskWorkConfig<TKey>.ScheduleJobDelegate scheduleJobDelegate)
        {
            JobTaskWorkConfig<TKey> config = new JobTaskWorkConfig<TKey>(scheduleJobDelegate, System, false);
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
        protected JobTaskWorkConfig<TKey> ConfigureUpdateJob(JobTaskWorkConfig<TKey>.ScheduleJobDelegate scheduleJobDelegate)
        {
            JobTaskWorkConfig<TKey> config = new JobTaskWorkConfig<TKey>(scheduleJobDelegate, System, false);
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
        protected JobTaskWorkConfig<TKey> ConfigureCancelJob(JobTaskWorkConfig<TKey>.ScheduleJobDelegate scheduleJobDelegate)
        {
            JobTaskWorkConfig<TKey> config = new JobTaskWorkConfig<TKey>(scheduleJobDelegate, System, true);
            m_CancelJobData.Add(config);
            return config;
        }

        private JobHandle Populate(JobHandle dependsOn)
        {
            Debug_EnsureNoMainThreadWorkCurrentlyActive();
            return m_PopulateJobData.BulkScheduleParallel(dependsOn, JobTaskWorkConfig<TKey>.PREPARE_AND_SCHEDULE_SCHEDULE_DELEGATE);
        }

        private JobHandle Update(JobHandle dependsOn)
        {
            Debug_EnsureNoMainThreadWorkCurrentlyActive();
            return m_UpdateJobData.BulkScheduleParallel(dependsOn, JobTaskWorkConfig<TKey>.PREPARE_AND_SCHEDULE_SCHEDULE_DELEGATE);
        }

        private JobHandle Cancel(JobHandle dependsOn)
        {
            Debug_EnsureNoMainThreadWorkCurrentlyActive();
            return m_CancelJobData.BulkScheduleParallel(dependsOn, JobTaskWorkConfig<TKey>.PREPARE_AND_SCHEDULE_SCHEDULE_DELEGATE);
        }

        private JobHandle Consolidate(JobHandle dependsOn)
        {
            Debug_EnsureNoMainThreadWorkCurrentlyActive();
            JobHandle cancelAccess = CancelData.AccessController.AcquireAsync(AccessType.SharedRead);
            JobHandle consolidateHandle = m_InstanceDataLookup.ConsolidateForFrame(JobHandle.CombineDependencies(dependsOn, cancelAccess), CancelData);
            CancelData.AccessController.ReleaseAsync(consolidateHandle);
            
            return consolidateHandle;
        }
        
        protected void RegisterSubTaskDriver(AbstractTaskDriver<TKey> subTaskDriver)
        {
            m_SubTaskDrivers.Add(subTaskDriver);
            subTaskDriver.AssignParent(this);
        }

        private void AssignParent(AbstractTaskDriver<TKey> parentTaskDriver)
        {
            Debug_EnsureNoParent();
            m_Parent = parentTaskDriver;
            CancelData.Dispose();
            CancelData = m_Parent.CancelData;
        }
        
        protected VirtualData<TKey, TInstance> GetData<TInstance>()
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            return m_InstanceDataLookup.GetData<TInstance>();
        }

        protected VirtualData<TKey, TInstance> CreateData<TInstance>(params AbstractVirtualData<TKey>[] sources)
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            VirtualData<TKey, TInstance> virtualData = VirtualData<TKey, TInstance>.Create(sources);
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
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNoParent()
        {
            if (m_Parent != null)
            {
                throw new InvalidOperationException($"{this} is trying to have a parent assigned but it already had {m_Parent} assigned!");
            }
        }
    }
}
