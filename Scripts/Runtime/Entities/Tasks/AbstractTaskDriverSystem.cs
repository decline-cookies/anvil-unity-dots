using Anvil.Unity.DOTS.Data;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract class AbstractTaskDriverSystem<TTaskDriverSystem, TTaskDriver, TKey, TSource, TResult> : AbstractTaskDriverSystem,
                                                                                                             ITaskDriverSystem<TTaskDriverSystem>
        where TTaskDriverSystem : AbstractTaskDriverSystem<TTaskDriverSystem, TTaskDriver, TKey, TSource, TResult>
        where TTaskDriver : AbstractTaskDriver<TTaskDriver, TKey, TSource, TResult>
        where TKey : struct, IEquatable<TKey>
        where TSource : struct, ILookupValue<TKey>
        where TResult : struct, ILookupValue<TKey>
    {
        private readonly VirtualData<TKey, TSource> m_SourceData;
        private readonly HashSet<ITaskDriver> m_TaskDrivers = new HashSet<ITaskDriver>();

        // ReSharper disable once InconsistentNaming
        private event Action<TTaskDriverSystem> m_OnCreated;
        private bool m_IsCreated;


        public event Action<TTaskDriverSystem> OnCreated
        {
            add
            {
                if (m_IsCreated)
                {
                    value((TTaskDriverSystem)this);
                }
                else
                {
                    m_OnCreated += value;
                }
            }
            remove => m_OnCreated -= value;
        }

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
            m_IsCreated = true;
            m_OnCreated?.Invoke((TTaskDriverSystem)this);
        }

        protected override void OnDestroy()
        {
            m_SourceData.Dispose();
            m_OnCreated = null;

            foreach (ITaskDriver taskDriver in m_TaskDrivers)
            {
                taskDriver.Dispose();
            }

            base.OnDestroy();
        }

        public TTaskDriver CreateTaskDriver(AbstractTaskDriver<TTaskDriver, TKey, TSource, TResult>.PopulateEntitiesFunction populateEntitiesFunction)
        {
            TTaskDriver taskDriver = (TTaskDriver)Activator.CreateInstance(typeof(TTaskDriver), this, m_SourceData, populateEntitiesFunction);
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
            Dependency = updateHandle;
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
            foreach (ITaskDriver taskDriver in m_TaskDrivers)
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
            foreach (ITaskDriver taskDriver in m_TaskDrivers)
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

    public interface ITaskDriverSystem<out TTaskDriverSystem>
        where TTaskDriverSystem : AbstractTaskDriverSystem
    {
        event Action<TTaskDriverSystem> OnCreated;
    }
}
