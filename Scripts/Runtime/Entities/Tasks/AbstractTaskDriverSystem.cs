using Anvil.Unity.DOTS.Data;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract class AbstractTaskDriverSystem<TKey, TInstance> : AbstractAnvilSystemBase
        where TKey : struct, IEquatable<TKey>
        where TInstance : struct, ILookupData<TKey>
    {
        private readonly HashSet<AbstractTaskDriver> m_TaskDrivers = new HashSet<AbstractTaskDriver>();
        
        protected DeferredNativeArray<TInstance> ArrayForScheduling
        {
            get => InstanceData.ArrayForScheduling;
        }

        //TODO: Could use this to get the batch strategy
        protected int BatchSize
        {
            get => InstanceData.BatchSize;
        }

        internal VirtualData<TKey, TInstance> InstanceData
        {
            get;
        }
        protected AbstractTaskDriverSystem()
        {
            //TODO: Do we want to assign a batch strategy or get it when scheduling?
            //TODO: How to handle choosing strategy?
            InstanceData = new VirtualData<TKey, TInstance>(BatchStrategy.MaximizeChunk);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
        }

        protected override void OnDestroy()
        {
            InstanceData.Dispose();

            foreach (AbstractTaskDriver taskDriver in m_TaskDrivers)
            {
                taskDriver.Dispose();
            }

            base.OnDestroy();
        }

        internal void AddTaskDriver(AbstractTaskDriver taskDriver)
        {
            m_TaskDrivers.Add(taskDriver);
        }
        
        protected override void OnUpdate()
        {
            //Have drivers be given the chance to add to the Instance Data
            JobHandle driversPopulateHandle = PopulateDrivers(Dependency);

            //Consolidate our instance data to operate on it
            JobHandle consolidateInstanceHandle = InstanceData.ConsolidateForFrame(driversPopulateHandle);

            //TODO: Bad name. (AcquireProcessorAsync or AcquireForWork) Think harder. Mike said PrepareForWorkAndAcquire 
            //Get the updater struct
            JobHandle jobDataUpdaterHandle = InstanceData.AcquireForUpdate(out JobInstanceUpdater<TKey, TInstance> jobDataUpdater);

            //Allow the generic work to happen in the derived class
            JobHandle updateInstancesHandle = UpdateInstances(JobHandle.CombineDependencies(consolidateInstanceHandle, jobDataUpdaterHandle), ref jobDataUpdater);

            //Release the updater struct
            InstanceData.ReleaseForUpdate(updateInstancesHandle);
            
            //Have drivers consolidate their result data
            JobHandle driversConsolidateHandle = ConsolidateDrivers(updateInstancesHandle);

            //Ensure this system's dependency is written back
            Dependency = driversConsolidateHandle;
        }

        //TODO: Can we merge code with Consolidate?
        private JobHandle PopulateDrivers(JobHandle dependsOn)
        {
            int taskDriversCount = m_TaskDrivers.Count;
            if (taskDriversCount <= 0)
            {
                return dependsOn;
            }

            NativeArray<JobHandle> taskDriverDependencies = new NativeArray<JobHandle>(taskDriversCount, Allocator.Temp);
            int index = 0;
            foreach (AbstractTaskDriver taskDriver in m_TaskDrivers)
            {
                taskDriverDependencies[index] = taskDriver.Populate(dependsOn);
                index++;
            }

            return JobHandle.CombineDependencies(taskDriverDependencies);
        }

        private JobHandle ConsolidateDrivers(JobHandle dependsOn)
        {
            int taskDriversCount = m_TaskDrivers.Count;
            if (taskDriversCount <= 0)
            {
                return dependsOn;
            }

            NativeArray<JobHandle> taskDriverDependencies = new NativeArray<JobHandle>(taskDriversCount, Allocator.Temp);
            int index = 0;
            foreach (AbstractTaskDriver taskDriver in m_TaskDrivers)
            {
                taskDriverDependencies[index] = taskDriver.Consolidate(dependsOn);
                index++;
            }

            return JobHandle.CombineDependencies(taskDriverDependencies);
        }

        protected abstract JobHandle UpdateInstances(JobHandle dependsOn, ref JobInstanceUpdater<TKey, TInstance> jobInstanceUpdater);
    }
}
