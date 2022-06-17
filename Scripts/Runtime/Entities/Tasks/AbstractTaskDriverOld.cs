// using Anvil.CSharp.Core;
// using Anvil.Unity.DOTS.Data;
// using System;
// using System.Collections.Generic;
// using Unity.Entities;
// using Unity.Jobs;
//
// namespace Anvil.Unity.DOTS.Entities
// {
//     public abstract class AbstractTaskDriver<TTaskDriverSystem, TKey, TInstance, TResult> : AbstractTaskDriver
//         where TTaskDriverSystem : AbstractTaskDriverSystem<TKey, TInstance>
//         where TKey : struct, IEquatable<TKey>
//         where TInstance : struct, ILookupData<TKey>
//         where TResult : struct, ILookupData<TKey>
//     {
//         // public delegate JobHandle PopulateAsyncDelegate(JobHandle dependsOn, JobInstanceWriter<TInstance> jobInstanceWriter, JobResultWriter<TResult> jobResultWriter);
//         //
//         // public delegate JobHandle PopulateEntitiesAsyncDelegate(JobHandle dependsOn, JobInstanceWriterEntities<TInstance> jobInstanceWriter, JobResultWriter<TResult> jobResultWriter);
//         
//         public delegate JobHandle PopulateDelegate(PopulateData populateData);
//
//         private readonly VirtualData<TKey, TInstance> m_InstanceData;
//         private readonly VirtualData<TKey, TResult> m_ResultData;
//         private readonly AbstractPopulator<TKey, TInstance, TResult> m_Populator;
//         private readonly 
//
//         public TTaskDriverSystem System
//         {
//             get;
//         }
//         
//         protected AbstractTaskDriver(World world, PopulateDelegate populateDelegate) : this(world)
//         {
//             m_Populator = new EntitiesAsyncPopulator<TTaskDriverSystem, TKey, TInstance, TResult>(populateDelegate);
//             
//         }
//         
//         private AbstractTaskDriver(World world) : base(world)
//         {
//             System = World.GetOrCreateSystem<TTaskDriverSystem>();
//             System.AddTaskDriver(this);
//             
//             m_InstanceData = System.InstanceData;
//             //TODO: How to specify?
//             m_ResultData = new VirtualData<TKey, TResult>(BatchStrategy.MaximizeChunk, m_InstanceData);
//            
//         }
//
//         protected override void DisposeSelf()
//         {
//             m_ResultData.Dispose();
//
//             base.DisposeSelf();
//         }
//         
//         public void Cancel(Entity entity)
//         {
//             //Add entity to pending entities to cancel
//             //Job that gets scheduled to look up those entities and cancel them
//         }
//
//         //TODO: Cancel, plus cancel children
//         
//         protected TChildTaskDriver RegisterChildTaskDriver<TChildTaskDriver>(TChildTaskDriver childTaskDriver)
//             where TChildTaskDriver : AbstractTaskDriver
//         {
//             base.RegisterChildTaskDriver(childTaskDriver);
//             return childTaskDriver;
//         }
//
//         internal sealed override JobHandle Populate(JobHandle dependsOn)
//         {
//             return m_Populator.Populate(dependsOn, m_InstanceData, m_ResultData);
//         }
//
//         internal sealed override JobHandle Consolidate(JobHandle dependsOn)
//         {
//             JobHandle consolidateResultHandle = m_ResultData.ConsolidateForFrame(dependsOn);
//             //TODO: Could add a hook for user processing
//
//             return consolidateResultHandle;
//         }
//     }
//
//     public abstract class AbstractTaskDriver : AbstractAnvilBase
//     {
//         private readonly List<AbstractTaskDriver> m_ChildTaskDrivers = new List<AbstractTaskDriver>();
//
//         public World World
//         {
//             get;
//         }
//         
//         protected AbstractTaskDriver(World world)
//         {
//             World = world;
//         }
//
//         protected override void DisposeSelf()
//         {
//             //TODO: Dispose children?
//             base.DisposeSelf();
//         }
//
//         protected void RegisterChildTaskDriver(AbstractTaskDriver childTaskDriver)
//         {
//             m_ChildTaskDrivers.Add(childTaskDriver);
//         }
//         
//
//         internal abstract JobHandle Populate(JobHandle dependsOn);
//         internal abstract JobHandle Consolidate(JobHandle dependsOn);
//     }
// }
