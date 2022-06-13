using Anvil.Unity.DOTS.Data;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract class AbstractTaskDriverSystem<TTaskDriver, TKey, TSource, TResult> : AbstractTaskDriverSystem
        where TTaskDriver : AbstractTaskDriver<TKey, TSource, TResult>
        where TKey : struct, IEquatable<TKey>
        where TSource : struct, ILookupValue<TKey>
        where TResult : struct, ILookupValue<TKey>
    {
        private class DeferredTaskDriver
        {
            public readonly AbstractPopulator<TKey, TSource, TResult> Populator;
            public readonly Action<TTaskDriver> Callback;

            public DeferredTaskDriver(AbstractPopulator<TKey, TSource, TResult> populator, Action<TTaskDriver> callback)
            {
                Populator = populator;
                Callback = callback;
            }
        }
        
        private readonly VirtualData<TKey, TSource> m_SourceData;
        private readonly HashSet<TTaskDriver> m_TaskDrivers = new HashSet<TTaskDriver>();
        private readonly List<DeferredTaskDriver> m_DeferredTaskDrivers = new List<DeferredTaskDriver>();

        protected DeferredNativeArray<TSource> ArrayForScheduling
        {
            get => m_SourceData.ArrayForScheduling;
        }

        //TODO: Could use this to get the batch strategy
        protected int BatchSize
        {
            get => m_SourceData.BatchSize;
        }

        protected AbstractTaskDriverSystem()
        {
            //TODO: Do we want to assign a batch strategy or get it when scheduling?
            //TODO: How to handle choosing strategy?
            m_SourceData = new VirtualData<TKey, TSource>(BatchStrategy.MaximizeChunk);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            CreateDeferredTaskDrivers();
        }

        protected override void OnDestroy()
        {
            m_SourceData.Dispose();

            foreach (TTaskDriver taskDriver in m_TaskDrivers)
            {
                taskDriver.Dispose();
            }
            
            m_DeferredTaskDrivers.Clear();

            base.OnDestroy();
        }
        
        public void CreateTaskDriver(AbstractPopulator<TKey, TSource, TResult> populator, Action<TTaskDriver> onTaskDriverCreated)
        {
            if (IsCreated)
            {
                onTaskDriverCreated?.Invoke(CreateTaskDriver(populator));
            }
            else
            {
                m_DeferredTaskDrivers.Add(new DeferredTaskDriver(populator, onTaskDriverCreated));
            }
        }

        private void CreateDeferredTaskDrivers()
        {
            foreach (DeferredTaskDriver deferredTaskDriver in m_DeferredTaskDrivers)
            {
                deferredTaskDriver.Callback?.Invoke(CreateTaskDriver(deferredTaskDriver.Populator));
            }
            m_DeferredTaskDrivers.Clear();
        }

        private TTaskDriver CreateTaskDriver(AbstractPopulator<TKey, TSource, TResult> populator)
        {
            TTaskDriver taskDriver = (TTaskDriver)Activator.CreateInstance(typeof(TTaskDriver), this, m_SourceData, populator);
            m_TaskDrivers.Add(taskDriver);
            return taskDriver;
        }

        protected override void OnUpdate()
        {
            //Update any drivers that this system spawned, this gives them a chance to add to the source data
            JobHandle driversPopulateHandle = PopulateDrivers(Dependency);

            //Consolidate our source data to operate on it
            JobHandle consolidateHandle = m_SourceData.ConsolidateForFrame(driversPopulateHandle);

            //TODO: Bad name. (AcquireProcessorAsync or AcquireForWork) Think harder. Mike said PrepareForWorkAndAcquire 
            //Get the source reader struct
            JobHandle sourceReaderHandle = m_SourceData.AcquireForWork(out JobSourceReader<TKey, TSource> jobSourceReader);

            //Allow the generic work to happen in the derived class
            JobHandle updateHandle = Update(JobHandle.CombineDependencies(consolidateHandle, sourceReaderHandle), ref jobSourceReader);

            //Release the source reader struct
            m_SourceData.ReleaseForWork(updateHandle);

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
            foreach (TTaskDriver taskDriver in m_TaskDrivers)
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
            foreach (TTaskDriver taskDriver in m_TaskDrivers)
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
        protected bool IsCreated
        {
            get;
            private set;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            IsCreated = true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }


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
