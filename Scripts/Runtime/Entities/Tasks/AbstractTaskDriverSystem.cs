using Anvil.Unity.DOTS.Data;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract class AbstractTaskDriverSystem<TKey, TSource> : AbstractTaskDriverSystem
        where TKey : struct, IEquatable<TKey>
        where TSource : struct, ILookupValue<TKey>
    {
        private readonly HashSet<AbstractTaskDriver> m_TaskDrivers = new HashSet<AbstractTaskDriver>();
        
        protected DeferredNativeArray<TSource> ArrayForScheduling
        {
            get => SourceData.ArrayForScheduling;
        }

        //TODO: Could use this to get the batch strategy
        protected int BatchSize
        {
            get => SourceData.BatchSize;
        }

        internal VirtualData<TKey, TSource> SourceData
        {
            get;
        }

        protected AbstractTaskDriverSystem()
        {
            //TODO: Do we want to assign a batch strategy or get it when scheduling?
            //TODO: How to handle choosing strategy?
            SourceData = new VirtualData<TKey, TSource>(BatchStrategy.MaximizeChunk);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
        }

        protected override void OnDestroy()
        {
            SourceData.Dispose();

            foreach (AbstractTaskDriver taskDriver in m_TaskDrivers)
            {
                taskDriver.Dispose();
            }

            base.OnDestroy();
        }
        
        protected override void OnUpdate()
        {
            //Update any drivers that this system spawned, this gives them a chance to add to the source data
            JobHandle driversPopulateHandle = PopulateDrivers(Dependency);

            //Consolidate our source data to operate on it
            //TODO: Allow for optional State data
            // JobHandle consolidateStateHandle = m_StateData.ConsolidateForFrame(driversPopulateHandle);
            JobHandle consolidateStateHandle = default;
            JobHandle consolidateSourceHandle = SourceData.ConsolidateForFrame(driversPopulateHandle);

            //TODO: Bad name. (AcquireProcessorAsync or AcquireForWork) Think harder. Mike said PrepareForWorkAndAcquire 
            //Get the source reader struct
            JobHandle sourceReaderHandle = SourceData.AcquireForWork(out JobSourceReader<TKey, TSource> jobSourceReader);

            //Allow the generic work to happen in the derived class
            JobHandle updateHandle = Update(JobHandle.CombineDependencies(consolidateSourceHandle, consolidateStateHandle, sourceReaderHandle), ref jobSourceReader);

            //Release the source reader struct
            SourceData.ReleaseForWork(updateHandle);

            JobHandle driversConsolidateHandle = ConsolidateDrivers(updateHandle);

            //Ensure this system's dependency is written back
            Dependency = driversConsolidateHandle;
        }

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

        protected abstract JobHandle Update(JobHandle dependsOn, ref JobSourceReader<TKey, TSource> jobSourceReader);
    }

    public abstract class AbstractTaskDriverSystem : AbstractAnvilSystemBase
    {
        //TODO: Figure out system ownership
        public EntityQuery GetEntityQuery(params ComponentType[] componentTypes)
        {
            return base.GetEntityQuery(componentTypes);
        }

        public EntityQuery GetEntityQuery(NativeArray<ComponentType> componentTypes)
        {
            return base.GetEntityQuery(componentTypes);
        }

        public EntityQuery GetEntityQuery(params EntityQueryDesc[] queryDesc)
        {
            return base.GetEntityQuery(queryDesc);
        }
    }
}
