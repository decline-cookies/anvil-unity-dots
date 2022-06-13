using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Data;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract class AbstractTaskDriver<TKey, TSource, TResult> : AbstractTaskDriver
        where TKey : struct, IEquatable<TKey>
        where TSource : struct, ILookupValue<TKey>
        where TResult : struct, ILookupValue<TKey>
    {
        private readonly VirtualData<TKey, TSource> m_SourceData;
        private readonly VirtualData<TKey, TResult> m_ResultData;
        private readonly AbstractPopulator<TKey, TSource, TResult> m_Populator;

        protected AbstractTaskDriver(AbstractTaskDriverSystem owningSystem,
                                     VirtualData<TKey, TSource> sourceData,
                                     AbstractPopulator<TKey, TSource, TResult> populator) : base(owningSystem)
        {
            m_SourceData = sourceData;
            m_Populator = populator;
            m_Populator.TaskDriver = this;
            //TODO: How to specify?
            m_ResultData = new VirtualData<TKey, TResult>(BatchStrategy.MaximizeChunk, sourceData);
        }

        protected override void DisposeSelf()
        {
            m_ResultData.Dispose();
            m_Populator.Dispose();

            base.DisposeSelf();
        }

        protected void CreateChildTaskDriver<TChildTaskDriverSystem, TChildTaskDriver, TChildKey, TChildSource, TChildResult>(AbstractPopulator<TChildKey, TChildSource, TChildResult> populator)
            where TChildTaskDriverSystem : AbstractTaskDriverSystem<TChildTaskDriver, TChildKey, TChildSource, TChildResult>
            where TChildTaskDriver : AbstractTaskDriver<TChildKey, TChildSource, TChildResult>
            where TChildKey : struct, IEquatable<TChildKey>
            where TChildSource : struct, ILookupValue<TChildKey>
            where TChildResult : struct, ILookupValue<TChildKey>
        {
            TChildTaskDriverSystem system = World.GetOrCreateSystem<TChildTaskDriverSystem>();
            system.CreateTaskDriver(populator, System_OnCreateTaskDriverComplete);
        }

        private void System_OnCreateTaskDriverComplete(AbstractTaskDriver taskDriver)
        {
            AddChildTaskDriver(taskDriver);
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
        protected World World
        {
            get;
        }
        
        protected AbstractTaskDriverSystem System
        {
            get;
        }
        
        private readonly List<AbstractTaskDriver> m_ChildTaskDrivers = new List<AbstractTaskDriver>();

        protected AbstractTaskDriver(AbstractTaskDriverSystem owningSystem)
        {
            System = owningSystem;
            World = System.World;
        }

        protected override void DisposeSelf()
        {
            //TODO: Dispose children?
            base.DisposeSelf();
        }

        protected void AddChildTaskDriver(AbstractTaskDriver childTaskDriver)
        {
            m_ChildTaskDrivers.Add(childTaskDriver);
        }
        
        //TODO: Cancel, plus cancel children


        internal abstract JobHandle Populate(JobHandle dependsOn);
        internal abstract JobHandle Consolidate(JobHandle dependsOn);
    }
}
