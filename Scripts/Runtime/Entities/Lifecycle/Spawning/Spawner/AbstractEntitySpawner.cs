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

        protected EntityManager EntityManager { get; private set; }

        protected EntityArchetype EntityArchetype { get; private set; }
        
        protected AbstractEntitySpawner()
        {
            m_DefinitionsToSpawn = new AccessControlledValue<UnsafeTypedStream<T>>(new UnsafeTypedStream<T>(Allocator.Persistent));
            // ReSharper disable once SuggestVarOrType_SimpleTypes
            using var handle = m_DefinitionsToSpawn.AcquireWithHandle(AccessType.ExclusiveWrite);
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            m_MainThreadLaneWriter = handle.Value.AsLaneWriter(ParallelAccessUtil.CollectionIndexForMainThread());
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

        protected JobHandle AcquireAsync(AccessType accessType, out UnsafeTypedStream<T> definitionsToSpawn)
        {
            return m_DefinitionsToSpawn.AcquireAsync(accessType, out definitionsToSpawn);
        }

        protected void ReleaseAsync(JobHandle dependsOn)
        {
            m_DefinitionsToSpawn.ReleaseAsync(dependsOn);
        }

        public JobHandle Schedule(JobHandle dependsOn,
                                  ref EntityCommandBuffer ecb,
                                  NativeParallelHashMap<long, EntityArchetype> entityArchetypeLookup)
        {
            JobHandle definitionsHandle = m_DefinitionsToSpawn.AcquireAsync(AccessType.ExclusiveWrite, out UnsafeTypedStream<T> definitions);
            dependsOn = JobHandle.CombineDependencies(definitionsHandle, dependsOn);

            dependsOn = ScheduleSpawnJob(dependsOn, definitions, ref ecb);

            m_DefinitionsToSpawn.ReleaseAsync(dependsOn);
            return dependsOn;
        }

        protected abstract JobHandle ScheduleSpawnJob(JobHandle dependsOn,
                                                      UnsafeTypedStream<T> spawnDefinitions,
                                                      ref EntityCommandBuffer ecb);
    }
}
