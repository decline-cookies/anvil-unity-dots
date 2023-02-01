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
        

        private readonly AccessControlledValue<UnsafeTypedStream<T>> m_DefinitionsToSpawn;
        private readonly UnsafeTypedStream<T>.LaneWriter m_MainThreadLaneWriter;
        private readonly UnsafeTypedStream<T>.Reader m_Reader;

        protected EntityManager EntityManager { get; private set; }
        
        protected EntityArchetype EntityArchetype { get; private set; }

        [SuppressMessage("ReSharper", "PossiblyImpureMethodCallOnReadonlyVariable")]
        protected AbstractEntitySpawner()
        {
            m_DefinitionsToSpawn = new AccessControlledValue<UnsafeTypedStream<T>>(new UnsafeTypedStream<T>(Allocator.Persistent));
            // ReSharper disable once SuggestVarOrType_SimpleTypes
            using var handle = m_DefinitionsToSpawn.AcquireWithHandle(AccessType.ExclusiveWrite);
            m_MainThreadLaneWriter = handle.Value.AsLaneWriter(ParallelAccessUtil.CollectionIndexForMainThread());
            m_Reader = handle.Value.AsReader();
        }

        public void Init(EntityManager entityManager, EntityArchetype entityArchetype)
        {
            EntityManager = entityManager;
            EntityArchetype = entityArchetype;
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

        protected void InternalSpawn(NativeArray<T> elements)
        {
            // ReSharper disable once SuggestVarOrType_SimpleTypes
            using var handle = m_DefinitionsToSpawn.AcquireWithHandle(AccessType.SharedWrite);
            foreach (T element in elements)
            {
                m_MainThreadLaneWriter.Write(element);
            }
        }
        
        public JobHandle Schedule(JobHandle dependsOn, 
                                  ref EntityCommandBuffer ecb, 
                                  NativeParallelHashMap<long, EntityArchetype> entityArchetypeLookup)
        {
            JobHandle definitionsHandle = m_DefinitionsToSpawn.AcquireAsync(AccessType.SharedRead, out UnsafeTypedStream<T> definitions);
            dependsOn = JobHandle.CombineDependencies(definitionsHandle, dependsOn);

            dependsOn = ScheduleSpawnJob(dependsOn, in m_Reader, ref ecb);
            
            m_DefinitionsToSpawn.ReleaseAsync(dependsOn);
            return dependsOn;
        }

        protected abstract JobHandle ScheduleSpawnJob(JobHandle dependsOn, 
                                                      in UnsafeTypedStream<T>.Reader reader,
                                                      ref EntityCommandBuffer ecb);
    }
}
