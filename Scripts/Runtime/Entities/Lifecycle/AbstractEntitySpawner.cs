using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal abstract class AbstractEntitySpawner<T> : AbstractAnvilBase,
                                                       IEntitySpawner
        where T : unmanaged
    {
        protected readonly EntityManager m_EntityManager;
        protected readonly EntityArchetype m_EntityArchetype;
        protected readonly long m_EntityArchetypeHash;
        
        private readonly AccessControlledValue<UnsafeTypedStream<T>> m_DefinitionsToSpawn;
        private readonly UnsafeTypedStream<T>.LaneWriter m_MainThreadLaneWriter;
        private readonly UnsafeTypedStream<T>.Reader m_Reader;

        [SuppressMessage("ReSharper", "PossiblyImpureMethodCallOnReadonlyVariable")]
        protected AbstractEntitySpawner(EntityManager entityManager, EntityArchetype entityArchetype, long entityArchetypeHash)
        {
            m_EntityManager = entityManager;
            m_EntityArchetype = entityArchetype;
            m_EntityArchetypeHash = entityArchetypeHash;
            m_DefinitionsToSpawn = new AccessControlledValue<UnsafeTypedStream<T>>(new UnsafeTypedStream<T>(Allocator.Persistent));
            // ReSharper disable once SuggestVarOrType_SimpleTypes
            using var handle = m_DefinitionsToSpawn.AcquireWithHandle(AccessType.ExclusiveWrite);
            m_MainThreadLaneWriter = handle.Value.AsLaneWriter(ParallelAccessUtil.CollectionIndexForMainThread());
            m_Reader = handle.Value.AsReader();
        }
        
        protected override void DisposeSelf()
        {
            m_DefinitionsToSpawn.Dispose();
            base.DisposeSelf();
        }

        protected void InternalSpawn(T element)
        {
            // ReSharper disable once SuggestVarOrType_SimpleTypes
            using var handle = m_DefinitionsToSpawn.AcquireWithHandle(AccessType.SharedWrite);
            m_MainThreadLaneWriter.Write(element);
        }
        
        public JobHandle Schedule(JobHandle dependsOn, EntityCommandBuffer ecb, NativeParallelHashMap<long, EntityArchetype> entityArchetypeLookup)
        {
            JobHandle definitionsHandle = m_DefinitionsToSpawn.AcquireAsync(AccessType.SharedRead, out UnsafeTypedStream<TEntitySpawnDefinition> definitions);
            dependsOn = JobHandle.CombineDependencies(definitionsHandle, dependsOn);
            
            
        }

        protected abstract JobHandle ScheduleSpawnJob(JobHandle dependsOn, 
                                                      ref UnsafeTypedStream<T>.Reader reader,
                                                      ref Enti);
    }
}
