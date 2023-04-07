using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;
using System.Linq;
using Unity.Burst;
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
        private NativeParallelHashMap<long, EntityArchetype> m_EntityArchetypes;
        private IReadOnlyAccessControlledValue<NativeParallelHashMap<long, Entity>> m_Prototypes;

        protected EntityManager EntityManager { get; private set; }


        protected bool MustDisableBurst { get; private set; }

        protected int MainThreadIndex { get; }

        protected AbstractEntitySpawner()
        {
            m_DefinitionsToSpawn = new AccessControlledValue<UnsafeTypedStream<T>>(new UnsafeTypedStream<T>(Allocator.Persistent));
            MainThreadIndex = ParallelAccessUtil.CollectionIndexForMainThread();
        }

        public void Init(
            EntityManager entityManager,
            NativeParallelHashMap<long, EntityArchetype> entityArchetypes,
            IReadOnlyAccessControlledValue<NativeParallelHashMap<long, Entity>> prototypes,
            bool mustDisableBurst)
        {
            EntityManager = entityManager;
            m_EntityArchetypes = entityArchetypes;
            m_Prototypes = prototypes;

            //TODO: #86 - When upgrading to Entities 1.0 we can use an unmanaged shared component which will let us use the job in burst
            MustDisableBurst = mustDisableBurst;
        }

        protected override void DisposeSelf()
        {
            m_DefinitionsToSpawn.Dispose();
            base.DisposeSelf();
        }

        protected EntitySpawnHelper AcquireEntitySpawnHelper()
        {
            return new EntitySpawnHelper(m_EntityArchetypes, m_Prototypes.AcquireReadOnly());
        }

        public void ReleaseEntitySpawnHelper()
        {
            m_Prototypes.Release();
        }

        protected void InternalSpawn(T element)
        {
            // ReSharper disable once SuggestVarOrType_SimpleTypes
            using var handle = m_DefinitionsToSpawn.AcquireWithHandle(AccessType.SharedWrite);
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            handle.Value.AsLaneWriter(MainThreadIndex).Write(ref element);
        }

        protected void InternalSpawn(NativeArray<T> elements)
        {
            // ReSharper disable once SuggestVarOrType_SimpleTypes
            using var handle = m_DefinitionsToSpawn.AcquireWithHandle(AccessType.SharedWrite);
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            UnsafeTypedStream<T>.LaneWriter laneWriter = handle.Value.AsLaneWriter(MainThreadIndex);
            foreach (T element in elements)
            {
                laneWriter.Write(element);
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

        public JobHandle Schedule(
            JobHandle dependsOn,
            ref EntityCommandBuffer ecb)
        {
            dependsOn = JobHandle.CombineDependencies(
                dependsOn,
                m_DefinitionsToSpawn.AcquireAsync(AccessType.ExclusiveWrite, out UnsafeTypedStream<T> definitions),
                m_Prototypes.AcquireReadOnlyAsync(out var prototypes));

            EntitySpawnHelper entitySpawnHelper = new EntitySpawnHelper(m_EntityArchetypes, prototypes);

            dependsOn = ScheduleSpawnJob(dependsOn, definitions, entitySpawnHelper, ref ecb);

            m_DefinitionsToSpawn.ReleaseAsync(dependsOn);
            m_Prototypes.ReleaseAsync(dependsOn);
            return dependsOn;
        }

        protected abstract JobHandle ScheduleSpawnJob(
            JobHandle dependsOn,
            UnsafeTypedStream<T> spawnDefinitions,
            EntitySpawnHelper entitySpawnHelper,
            ref EntityCommandBuffer ecb);
    }
}
