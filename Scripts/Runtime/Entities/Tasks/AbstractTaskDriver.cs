using Anvil.CSharp.Command;
using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Debug = UnityEngine.Debug;

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
        internal static readonly BulkScheduleDelegate<AbstractTaskDriver> HANDLE_CANCELLED_SCHEDULE_DELEGATE = BulkSchedulingUtil.CreateSchedulingDelegate<BulkScheduleDelegate<AbstractTaskDriver>, AbstractTaskDriver>(nameof(HandleCancelled), BindingFlags.Instance | BindingFlags.NonPublic);
        internal static readonly BulkScheduleDelegate<AbstractTaskDriver> CONSOLIDATE_SCHEDULE_DELEGATE = BulkSchedulingUtil.CreateSchedulingDelegate<BulkScheduleDelegate<AbstractTaskDriver>, AbstractTaskDriver>(nameof(Consolidate), BindingFlags.Instance | BindingFlags.NonPublic);
        internal static readonly BulkScheduleDelegate<AbstractTaskDriver> CONSOLIDATE_CANCEL_SCHEDULE_DELEGATE = BulkSchedulingUtil.CreateSchedulingDelegate<BulkScheduleDelegate<AbstractTaskDriver>, AbstractTaskDriver>(nameof(ConsolidateCancel), BindingFlags.Instance | BindingFlags.NonPublic);
        internal static readonly BulkScheduleDelegate<AbstractTaskDriver> PROPAGATE_CANCEL_TO_SUB_TASK_DRIVERS_SCHEDULE_DELEGATE = BulkSchedulingUtil.CreateSchedulingDelegate<BulkScheduleDelegate<AbstractTaskDriver>, AbstractTaskDriver>(nameof(PropagateCancelToSubTaskDrivers), BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly VirtualDataLookup m_InstanceDataLookup;

        private readonly List<AbstractTaskDriver> m_SubTaskDrivers;
        private readonly List<JobTaskWorkConfig> m_PopulateJobData;
        private readonly List<JobTaskWorkConfig> m_UpdateJobData;
        private readonly List<JobTaskWorkConfig> m_HandleCancelledJobData;

        private MainThreadTaskWorkConfig m_ActiveMainThreadTaskWorkConfig;

        private readonly int m_Context;
        
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private bool m_HasCheckedUpdateJobsForDataLoss;
#endif

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

        internal CancelData CancelData
        {
            get;
        }

        protected AbstractTaskDriver(World world, AbstractTaskDriverSystem system)
        {
            World = world;
            System = system;

            //Generate a unique ID for this TaskDriver
            m_Context = System.GetNextID();

            m_InstanceDataLookup = new VirtualDataLookup();
            CancelData = new CancelData(this);

            m_SubTaskDrivers = new List<AbstractTaskDriver>();
            m_PopulateJobData = new List<JobTaskWorkConfig>();
            m_UpdateJobData = new List<JobTaskWorkConfig>();
            m_HandleCancelledJobData = new List<JobTaskWorkConfig>();

            System.RegisterTaskDriver(this);
        }

        protected override void DisposeSelf()
        {
            m_InstanceDataLookup.Dispose();
            CancelData.Dispose();

            foreach (AbstractTaskDriver childTaskDriver in m_SubTaskDrivers)
            {
                childTaskDriver.Dispose();
            }

            ReleaseMainThreadTaskWork();

            m_SubTaskDrivers.Clear();
            m_PopulateJobData.Clear();
            m_UpdateJobData.Clear();
            m_HandleCancelledJobData.Clear();

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
            m_ActiveMainThreadTaskWorkConfig = new MainThreadTaskWorkConfig(System, m_Context);
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
            JobTaskWorkConfig config = new JobTaskWorkConfig(scheduleJobDelegate, System, m_Context);
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
            JobTaskWorkConfig config = new JobTaskWorkConfig(scheduleJobDelegate, System, m_Context);
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
            JobTaskWorkConfig config = new JobTaskWorkConfig(scheduleJobDelegate, System, m_Context);
            m_HandleCancelledJobData.Add(config);
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
            JobTaskWorkConfig.Debug_EnsureNoDataLoss(this, m_InstanceDataLookup, m_UpdateJobData, ref m_HasCheckedUpdateJobsForDataLoss);
            return m_UpdateJobData.BulkScheduleParallel(dependsOn, JobTaskWorkConfig.PREPARE_AND_SCHEDULE_SCHEDULE_DELEGATE);
        }

        private JobHandle HandleCancelled(JobHandle dependsOn)
        {
            Debug_EnsureNoMainThreadWorkCurrentlyActive();
            return m_HandleCancelledJobData.BulkScheduleParallel(dependsOn, JobTaskWorkConfig.PREPARE_AND_SCHEDULE_SCHEDULE_DELEGATE);
        }

        private JobHandle Consolidate(JobHandle dependsOn)
        {
            Debug_EnsureNoMainThreadWorkCurrentlyActive();
            JobHandle cancelAccess = CancelData.AccessController.AcquireAsync(AccessType.SharedRead);
            JobHandle consolidateHandle = m_InstanceDataLookup.ConsolidateForFrame(JobHandle.CombineDependencies(dependsOn, cancelAccess), CancelData);
            CancelData.AccessController.ReleaseAsync(consolidateHandle);

            return consolidateHandle;
        }

        private JobHandle ConsolidateCancel(JobHandle dependsOn)
        {
            Debug_EnsureNoMainThreadWorkCurrentlyActive();
            JobHandle cancelAccess = CancelData.AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            JobHandle systemCancelAccess = System.CancelData.AccessController.AcquireAsync(AccessType.SharedWrite);

            ConsolidateTaskDriverCancelDataJob taskDriverCancelDataJob = new ConsolidateTaskDriverCancelDataJob(CancelData, 
                                                                                                                System.CancelData.CreateVDCancelWriter(m_Context));

            dependsOn = JobHandle.CombineDependencies(dependsOn,
                                                      cancelAccess,
                                                      systemCancelAccess);
            dependsOn = taskDriverCancelDataJob.Schedule(dependsOn);

            CancelData.AccessController.ReleaseAsync(dependsOn);
            System.CancelData.AccessController.ReleaseAsync(dependsOn);

            return dependsOn;
        }

        private JobHandle PropagateCancelToSubTaskDrivers(JobHandle dependsOn)
        {
            Debug_EnsureNoMainThreadWorkCurrentlyActive();
            
            int len = m_SubTaskDrivers.Count;
            if (len == 0)
            {
                return dependsOn;
            }
            
            dependsOn = JobHandle.CombineDependencies(dependsOn,
                                                      CancelData.AccessController.AcquireAsync(AccessType.SharedRead));

            NativeArray<JobHandle> dependencies = new NativeArray<JobHandle>(len, Allocator.Temp);
            
            for(int i = 0; i < len; ++i)
            {
                AbstractTaskDriver subTaskDriver = m_SubTaskDrivers[i];
                JobHandle subTaskDriverCancelAccess = subTaskDriver.CancelData.AccessController.AcquireAsync(AccessType.SharedWrite);
                PropagateCancelToSubTaskDriverJob propagateJob = new PropagateCancelToSubTaskDriverJob(CancelData.CreateVDReader(),
                                                                                                       subTaskDriver.CancelData.CreateVDCancelWriter(m_Context));
                JobHandle propagateHandle = propagateJob.ScheduleParallel(CancelData.ScheduleInfo,
                                                                          CancelData.MAX_ELEMENTS_PER_CHUNK,
                                                                          JobHandle.CombineDependencies(dependsOn, subTaskDriverCancelAccess));
                
                subTaskDriver.CancelData.AccessController.ReleaseAsync(propagateHandle);
                
                dependencies[i] = propagateHandle;
            }

            dependsOn = JobHandle.CombineDependencies(dependencies);
            
            CancelData.AccessController.ReleaseAsync(dependsOn);
            
            return dependsOn;
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

        protected VirtualData<TInstance> CreateData<TInstance>(VirtualDataIntent dataIntent, params AbstractVirtualData[] sources)
            where TInstance : unmanaged, IKeyedData
        {
            VirtualData<TInstance> virtualData = new VirtualData<TInstance>(dataIntent, sources);
            m_InstanceDataLookup.AddData(virtualData);
            
            //Add helper simple job to keep the data around each frame
            if (dataIntent == VirtualDataIntent.Persistent)
            {
                ConfigureUpdateJob(VirtualData<TInstance>.KeepPersistentJob.Schedule)
                   .ScheduleOn(virtualData, BatchStrategy.MaximizeChunk)
                   .RequireDataForUpdateAsync(virtualData);
            }

            return virtualData;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct PropagateCancelToSubTaskDriverJob : IAnvilJobForDefer
        {
            [ReadOnly] private readonly VDReader<VDContextID> m_ParentTaskDriverCancelReader;
            private VDCancelWriter m_SubTaskDriverCancelWriter;

            public PropagateCancelToSubTaskDriverJob(VDReader<VDContextID> parentTaskDriverCancelReader, 
                                                     VDCancelWriter subTaskDriverCancelWriter)
            {
                m_ParentTaskDriverCancelReader = parentTaskDriverCancelReader;
                m_SubTaskDriverCancelWriter = subTaskDriverCancelWriter;
            }

            public void InitForThread(int nativeThreadIndex)
            {
                m_SubTaskDriverCancelWriter.InitForThread(nativeThreadIndex);
            }

            public void Execute(int index)
            {
                VDContextID id = m_ParentTaskDriverCancelReader[index];
                m_SubTaskDriverCancelWriter.Cancel(new VDContextID(id.Entity));
            }
        }
        
        
        [BurstCompile]
        private struct ConsolidateTaskDriverCancelDataJob : IAnvilJob
        {
            private UnsafeTypedStream<VDContextID> m_Pending;
            private DeferredNativeArray<VDContextID> m_IterationTarget;
            private UnsafeParallelHashMap<VDContextID, bool> m_Lookup;
            private VDCancelWriter m_SystemCancelWriter;

            public ConsolidateTaskDriverCancelDataJob(CancelData taskDriverCancelData,
                                                      VDCancelWriter systemCancelWriter)
            {
                m_Pending = taskDriverCancelData.Pending;
                m_IterationTarget = taskDriverCancelData.IterationTarget;
                m_Lookup = taskDriverCancelData.Lookup;
                m_SystemCancelWriter = systemCancelWriter;
            }

            public void InitForThread(int nativeThreadIndex)
            {
                m_SystemCancelWriter.InitForThread(nativeThreadIndex);
            }

            public void Execute()
            {
                //Clear previously consolidated 
                m_Lookup.Clear();
                m_IterationTarget.Clear();

                //Get the new counts
                int pendingCount = m_Pending.Count();

                //Allocate memory for arrays based on counts
                NativeArray<VDContextID> iterationArray = m_IterationTarget.DeferredCreate(pendingCount);

                //Fast blit
                m_Pending.CopyTo(ref iterationArray);

                //Populate the lookup
                for (int i = 0; i < pendingCount; ++i)
                {
                    VDContextID id = iterationArray[i];
                    m_Lookup.TryAdd(id, true); 
                    //Also populate the system
                    m_SystemCancelWriter.Cancel(ref id);
                }

                //Clear pending for next frame
                m_Pending.Clear();
            }
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

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
