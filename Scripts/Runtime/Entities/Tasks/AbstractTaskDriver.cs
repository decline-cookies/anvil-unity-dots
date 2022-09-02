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
    public abstract class AbstractTaskDriver<TTaskDriverSystem, TTaskData, TResultDestinationType> : AbstractTaskDriver
        where TTaskDriverSystem : AbstractTaskDriverSystem<TTaskData>
        where TTaskData : unmanaged, IEntityProxyData, ITaskData
        where TResultDestinationType : Enum
    {
        private readonly VirtualDataLookup m_ResultsData;
        
        // - MOVED FROM OLD VIRTUAL DATA
        // private readonly byte m_ResultDestinationType;
        // private AbstractProxyDataStream m_Source;
        // protected Dictionary<byte, AbstractProxyDataStream> ResultDestinations { get; }
        // private const byte UNSET_RESULT_DESTINATION_TYPE = byte.MaxValue;
        // private VDResultsDestinationLookup m_ResultsDestinationLookup;
        // internal static ProxyDataStream<TData> Create()
        // {
        //     ProxyDataStream<TData> virtualData = new ProxyDataStream<TData>(UNSET_RESULT_DESTINATION_TYPE);
        //     return virtualData;
        // }
        //
        // internal static ProxyDataStream<TData> CreateAsResultsDestination<TResultDestinationType>(TResultDestinationType resultDestinationType, AbstractProxyDataStream source)
        //     where TResultDestinationType : Enum
        // {
        //     //TODO: Add an assert that this is valid - https://github.com/decline-cookies/anvil-unity-dots/pull/52/files#r960848904
        //     byte value = UnsafeUtility.As<TResultDestinationType, byte>(ref resultDestinationType);
        //     ProxyDataStream<TData> resultDestinationData = new ProxyDataStream<TData>(value);
        //     
        //     resultDestinationData.SetSource(source);
        //     source.AddResultDestination(value, resultDestinationData);
        //     
        //     return resultDestinationData;
        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        // [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        // private void Debug_EnsureContextIsSet(byte context)
        // {
        //     //TODO: Deal with actually handling this
        //     if (context == IDProvider.UNSET_ID)
        //     {
        //         throw new InvalidOperationException($"Context for {typeof(TData)} is not set!");
        //     }
        // }
        //
        // }
        //

        public ProxyDataStream<TTaskData> TaskData
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

        protected ProxyDataStream<TTaskResultData> CreateResultsData<TTaskResultData>(TResultDestinationType resultDestinationType)
            where TTaskResultData : unmanaged, IEntityProxyData
        {
            ProxyDataStream<TTaskResultData> virtualData = ProxyDataStream<TTaskResultData>.CreateAsResultsDestination(resultDestinationType, TaskData);
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
        private readonly byte m_Context;

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
            
            //Generate a Unique ID for this TaskDriver relative to its System
            m_Context = System.GetNextID();
            
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
