using Anvil.Unity.DOTS.Data;
using System;
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
            
            base.OnDestroy();
        }

        public TTaskDriver CreateTaskDriver(AbstractTaskDriver<TTaskDriver, TKey, TSource, TResult>.PopulateEntitiesFunction populateEntitiesFunction)
        {
            TTaskDriver taskDriver = (TTaskDriver)Activator.CreateInstance(typeof(TTaskDriver), this, m_SourceData, populateEntitiesFunction);
            return taskDriver;
        }

        protected override void OnUpdate()
        {
            //Update our source data
            JobHandle consolidateHandle = m_SourceData.ConsolidateForFrame(Dependency);

            //TODO: Bad name. (AcquireProcessorAsync or AcquireForWork) Think harder. Mike said PrepareForWorkAndAcquire 
            //Get the source reader struct
            JobHandle sourceReaderHandle = m_SourceData.AcquireForWork(out JobSourceReader<TKey, TSource> jobSourceReader);

            //Allow the generic work to happen in the derived class
            JobHandle updateHandle = Update(JobHandle.CombineDependencies(consolidateHandle, sourceReaderHandle), ref jobSourceReader);

            //Release the source reader struct
            m_SourceData.ReleaseForWork(updateHandle);

            //Ensure this system's dependency is written back
            Dependency = updateHandle;
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

    public interface ITaskDriverSystem<TTaskDriverSystem>
        where TTaskDriverSystem : AbstractTaskDriverSystem
    {
        event Action<TTaskDriverSystem> OnCreated;
    }
}
