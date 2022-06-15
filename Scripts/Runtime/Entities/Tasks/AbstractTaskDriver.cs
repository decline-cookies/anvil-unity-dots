using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Data;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract class AbstractTaskDriver<TTaskDriverSystem, TKey, TSource, TResult> : AbstractTaskDriver
        where TTaskDriverSystem : AbstractTaskDriverSystem<TKey, TSource>
        where TKey : struct, IEquatable<TKey>
        where TSource : struct, ILookupValue<TKey>
        where TResult : struct, ILookupValue<TKey>
    {
        public delegate JobHandle PopulateAsyncDelegate(JobHandle dependsOn, JobSourceWriter<TSource> sourceWriter, JobResultWriter<TResult> resultWriter);

        public delegate JobHandle PopulateEntitiesAsyncDelegate(JobHandle dependsOn, JobEntitiesSourceWriter<TSource> sourceWriter, JobResultWriter<TResult> resultWriter);

        private readonly VirtualData<TKey, TSource> m_SourceData;
        private readonly VirtualData<TKey, TResult> m_ResultData;
        private readonly AbstractPopulator<TKey, TSource, TResult> m_Populator;

        public TTaskDriverSystem System
        {
            get;
        }
        
        protected AbstractTaskDriver(World world, PopulateEntitiesAsyncDelegate populateDelegate) : this(world)
        {
            m_Populator = new EntitiesAsyncPopulator<TTaskDriverSystem, TKey, TSource, TResult>(populateDelegate);
        }
        
        protected AbstractTaskDriver(World world, PopulateAsyncDelegate populateDelegate) : this(world)
        {
            m_Populator = new AsyncPopulator<TTaskDriverSystem, TKey, TSource, TResult>(populateDelegate);
        }

        private AbstractTaskDriver(World world) : base(world)
        {
            System = World.GetOrCreateSystem<TTaskDriverSystem>();
            
            m_SourceData = System.SourceData;
            //TODO: How to specify?
            m_ResultData = new VirtualData<TKey, TResult>(BatchStrategy.MaximizeChunk, m_SourceData);
        }

        protected override void DisposeSelf()
        {
            m_ResultData.Dispose();

            base.DisposeSelf();
        }
        
        protected TChildTaskDriver RegisterChildTaskDriver<TChildTaskDriver>(TChildTaskDriver childTaskDriver)
            where TChildTaskDriver : AbstractTaskDriver
        {
            base.RegisterChildTaskDriver(childTaskDriver);
            return childTaskDriver;
        }

        internal sealed override JobHandle Populate(JobHandle dependsOn)
        {
            return m_Populator.Populate(dependsOn, m_SourceData, m_ResultData);
        }

        internal sealed override JobHandle Consolidate(JobHandle dependsOn)
        {
            JobHandle consolidateResultHandle = m_ResultData.ConsolidateForFrame(dependsOn);
            //TODO: Could add a hook for user processing

            return consolidateResultHandle;
        }
    }

    public abstract class AbstractTaskDriver : AbstractAnvilBase
    {
        private readonly List<AbstractTaskDriver> m_ChildTaskDrivers = new List<AbstractTaskDriver>();

        public World World
        {
            get;
        }
        
        protected AbstractTaskDriver(World world)
        {
            World = world;
        }

        protected override void DisposeSelf()
        {
            //TODO: Dispose children?
            base.DisposeSelf();
        }

        protected void RegisterChildTaskDriver(AbstractTaskDriver childTaskDriver)
        {
            m_ChildTaskDrivers.Add(childTaskDriver);
        }

        public void Cancel(Entity entity)
        {
            //Add entity to pending entities to cancel
            //Job that gets scheduled to look up those entities and cancel them
        }

        //TODO: Cancel, plus cancel children


        internal abstract JobHandle Populate(JobHandle dependsOn);
        internal abstract JobHandle Consolidate(JobHandle dependsOn);
    }
}
