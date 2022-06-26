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
    public abstract class AbstractTaskDriver<TTaskDriverSystem> : AbstractTaskDriver
        where TTaskDriverSystem : AbstractTaskDriverSystem
    {
        public new TTaskDriverSystem System
        {
            get;
        }

        protected AbstractTaskDriver(World world) : base(world)
        {
            System = World.GetOrCreateSystem<TTaskDriverSystem>();
            base.System = System;
            System.AddTaskDriver(this);

            ConstructData();
            ConstructChildTaskDrivers();
        }

        private void ConstructData()
        {
            InitData();
        }

        private void ConstructChildTaskDrivers()
        {
            InitChildTaskDrivers();
        }

        protected override void DisposeSelf()
        {
            base.DisposeSelf();
        }
    }

    public abstract class AbstractTaskDriver : AbstractAnvilBase
    {
        public class TaskDriverPopulateBulkScheduler : AbstractBulkScheduler<AbstractTaskDriver>
        {
            public TaskDriverPopulateBulkScheduler(List<AbstractTaskDriver> list) : base(list)
            {
            }

            protected override JobHandle ScheduleItem(AbstractTaskDriver item, JobHandle dependsOn)
            {
                return item.Populate(dependsOn);
            }
        }

        public class TaskDriverUpdateBulkScheduler : AbstractBulkScheduler<AbstractTaskDriver>
        {
            public TaskDriverUpdateBulkScheduler(List<AbstractTaskDriver> list) : base(list)
            {
            }

            protected override JobHandle ScheduleItem(AbstractTaskDriver item, JobHandle dependsOn)
            {
                return item.Update(dependsOn);
            }
        }

        public class TaskDriverConsolidateBulkScheduler : AbstractBulkScheduler<AbstractTaskDriver>
        {
            public TaskDriverConsolidateBulkScheduler(List<AbstractTaskDriver> list) : base(list)
            {
            }

            protected override JobHandle ScheduleItem(AbstractTaskDriver item, JobHandle dependsOn)
            {
                return item.Consolidate(dependsOn);
            }
        }

        private readonly VirtualDataLookup m_InstanceData;
        private readonly List<AbstractTaskDriver> m_ChildTaskDrivers;
        private readonly List<JobTaskWorkConfig> m_PopulateJobData;
        private readonly List<JobTaskWorkConfig> m_UpdateJobData;

        private readonly JobTaskWorkConfig.JobTaskWorkConfigBulkScheduler m_PopulateBulkScheduler;
        private readonly JobTaskWorkConfig.JobTaskWorkConfigBulkScheduler m_UpdateBulkScheduler;

        private MainThreadTaskWorkConfig m_ActiveMainThreadTaskWorkConfig;

        public World World
        {
            get;
        }

        public AbstractTaskDriverSystem System
        {
            get;
            protected set;
        }

        protected AbstractTaskDriver(World world)
        {
            World = world;
            m_InstanceData = new VirtualDataLookup();
            m_ChildTaskDrivers = new List<AbstractTaskDriver>();
            m_PopulateJobData = new List<JobTaskWorkConfig>();
            m_UpdateJobData = new List<JobTaskWorkConfig>();

            m_PopulateBulkScheduler = new JobTaskWorkConfig.JobTaskWorkConfigBulkScheduler(m_PopulateJobData);
            m_UpdateBulkScheduler = new JobTaskWorkConfig.JobTaskWorkConfigBulkScheduler(m_UpdateJobData);
        }

        protected override void DisposeSelf()
        {
            m_InstanceData.Dispose();

            foreach (AbstractTaskDriver childTaskDriver in m_ChildTaskDrivers)
            {
                childTaskDriver.Dispose();
            }

            ReleaseMainThreadTaskWork();

            m_ChildTaskDrivers.Clear();
            m_PopulateJobData.Clear();
            m_UpdateJobData.Clear();

            base.DisposeSelf();
        }

        //TODO: Cancel plus cancel children
        //TODO: Need to actually add the children


        protected abstract void InitData();
        protected abstract void InitChildTaskDrivers();

        public VirtualData<TKey, TInstance> GetInstanceData<TKey, TInstance>()
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


        //TODO: Some way to remove the populate function
        public JobTaskWorkConfig CreatePopulateJob(JobTaskWorkConfig.JobDataDelegate jobDataDelegate)
        {
            JobTaskWorkConfig config = new JobTaskWorkConfig(jobDataDelegate, System);
            m_PopulateJobData.Add(config);
            return config;
        }

        //TODO: Some way to remove the update Job
        protected JobTaskWorkConfig CreateUpdateJob(JobTaskWorkConfig.JobDataDelegate jobDataDelegate)
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

        public MainThreadTaskWorkConfig ConfigureMainThreadTaskWork()
        {
            DebugEnsureNoMainThreadWorkCurrentlyActive();
            m_ActiveMainThreadTaskWorkConfig = new MainThreadTaskWorkConfig(System);
            return m_ActiveMainThreadTaskWorkConfig;
        }

        public void ReleaseMainThreadTaskWork()
        {
            m_ActiveMainThreadTaskWorkConfig?.Release();
            m_ActiveMainThreadTaskWorkConfig = null;
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void DebugEnsureNoMainThreadWorkCurrentlyActive()
        {
            if (m_ActiveMainThreadTaskWorkConfig != null)
            {
                throw new InvalidOperationException($"Main Thread Task Work currently active, wait until after {nameof(ReleaseMainThreadTaskWork)} is called.");
            }
        }
    }
}
