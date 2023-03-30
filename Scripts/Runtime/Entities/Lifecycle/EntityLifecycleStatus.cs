using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class EntityLifecycleStatus : AbstractAnvilBase,
                                           IEntityLifecycleStatus
    {
        private readonly AccessControlledValue<NativeParallelHashSet<Entity>> m_Lookup;
        private readonly AccessControlledValue<NativeList<Entity>> m_ArrivedEntities;
        private readonly AccessControlledValue<NativeList<Entity>> m_DepartedEntities;
        private readonly AbstractEntityLifecycleStatusSystem m_OwningSystem;
        private readonly ComponentType[] m_QueryComponentTypes;

        private EntityQuery m_Query;

        public EntityLifecycleStatus(AbstractEntityLifecycleStatusSystem owningSystem, params ComponentType[] queryComponentTypes)
        {
            m_OwningSystem = owningSystem;
            m_QueryComponentTypes = queryComponentTypes;

            m_Lookup = new AccessControlledValue<NativeParallelHashSet<Entity>>(
                new NativeParallelHashSet<Entity>(
                    ChunkUtil.MaxElementsPerChunk<Entity>(),
                    Allocator.Persistent));

            m_ArrivedEntities = new AccessControlledValue<NativeList<Entity>>(new NativeList<Entity>(ChunkUtil.MaxElementsPerChunk<Entity>(), Allocator.Persistent));
            m_DepartedEntities = new AccessControlledValue<NativeList<Entity>>(new NativeList<Entity>(ChunkUtil.MaxElementsPerChunk<Entity>(), Allocator.Persistent));
        }

        protected override void DisposeSelf()
        {
            m_Lookup.Dispose();
            m_ArrivedEntities.Dispose();
            m_DepartedEntities.Dispose();
            base.DisposeSelf();
        }

        public void CreateQuery()
        {
            m_Query = m_OwningSystem.GetEntityQuery(m_QueryComponentTypes);
            m_Query.SetOrderVersionFilter();
        }
        
        //*************************************************************************************************************
        // PUBLIC API
        //*************************************************************************************************************

        public JobHandle AcquireArrivalsAsync(out NativeList<Entity> arrivals)
        {
            return m_ArrivedEntities.AcquireAsync(AccessType.SharedRead, out arrivals);
        }

        public JobHandle AcquireDeparturesAsync(out NativeList<Entity> departures)
        {
            return m_DepartedEntities.AcquireAsync(AccessType.SharedRead, out departures);
        }

        public void ReleaseArrivalsAsync(JobHandle dependsOn)
        {
            m_ArrivedEntities.ReleaseAsync(dependsOn);
        }

        public void ReleaseDeparturesAsync(JobHandle dependsOn)
        {
            m_DepartedEntities.ReleaseAsync(dependsOn);
        }

        public AccessControlledValue<NativeList<Entity>>.AccessHandle AcquireArrivals()
        {
            return m_ArrivedEntities.AcquireWithHandle(AccessType.SharedRead);
        }

        public AccessControlledValue<NativeList<Entity>>.AccessHandle AcquireDepartures()
        {
            return m_DepartedEntities.AcquireWithHandle(AccessType.SharedRead);
        }

        //*************************************************************************************************************
        // UPDATES
        //*************************************************************************************************************
        
        public JobHandle UpdateAsync(
            JobHandle dependsOn,
            ref NativeArray<Entity>.ReadOnly destroyedEntities)
        {
            dependsOn = ClearAsync(dependsOn);
            dependsOn = UpdateDepartedAsync(dependsOn, ref destroyedEntities);
            dependsOn = UpdateArrivedAsync(dependsOn);

            return dependsOn;
        }

        private JobHandle ClearAsync(JobHandle dependsOn)
        {
            dependsOn = JobHandle.CombineDependencies(
                dependsOn,
                m_ArrivedEntities.AcquireAsync(AccessType.ExclusiveWrite, out var arrivedEntities),
                m_DepartedEntities.AcquireAsync(AccessType.ExclusiveWrite, out var departedEntities));

            ClearJob clearJob = new ClearJob(arrivedEntities, departedEntities);
            dependsOn = clearJob.Schedule(dependsOn);

            m_ArrivedEntities.ReleaseAsync(dependsOn);
            m_DepartedEntities.ReleaseAsync(dependsOn);

            return dependsOn;
        }

        private JobHandle UpdateDepartedAsync(
            JobHandle dependsOn,
            ref NativeArray<Entity>.ReadOnly destroyedEntities)
        {
            dependsOn = JobHandle.CombineDependencies(
                dependsOn,
                m_Lookup.AcquireAsync(AccessType.ExclusiveWrite, out var lookup),
                m_DepartedEntities.AcquireAsync(AccessType.ExclusiveWrite, out var departedEntities));

            UpdateDepartedJob updateDepartedJob = new UpdateDepartedJob(
                destroyedEntities,
                lookup,
                departedEntities);
            dependsOn = updateDepartedJob.Schedule(dependsOn);

            m_Lookup.ReleaseAsync(dependsOn);
            m_DepartedEntities.ReleaseAsync(dependsOn);

            return dependsOn;
        }

        private JobHandle UpdateArrivedAsync(JobHandle dependsOn)
        {
            dependsOn = JobHandle.CombineDependencies(
                dependsOn,
                m_Lookup.AcquireAsync(AccessType.ExclusiveWrite, out var lookup),
                m_ArrivedEntities.AcquireAsync(AccessType.ExclusiveWrite, out var arrivedEntities));


            UpdateArrivedJob updateArrivedJob = new UpdateArrivedJob(
                m_OwningSystem.GetEntityTypeHandle(),
                lookup,
                arrivedEntities);
            dependsOn = updateArrivedJob.Schedule(m_Query, dependsOn);

            m_Lookup.ReleaseAsync(dependsOn);
            m_ArrivedEntities.ReleaseAsync(dependsOn);

            return dependsOn;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ClearJob : IJob
        {
            private NativeList<Entity> m_ArrivedEntities;
            private NativeList<Entity> m_DepartedEntities;

            public ClearJob(NativeList<Entity> arrivedEntities, NativeList<Entity> departedEntities)
            {
                m_ArrivedEntities = arrivedEntities;
                m_DepartedEntities = departedEntities;
            }

            public void Execute()
            {
                m_ArrivedEntities.Clear();
                m_DepartedEntities.Clear();
            }
        }

        [BurstCompile]
        private struct UpdateDepartedJob : IJob
        {
            [ReadOnly] private readonly NativeArray<Entity>.ReadOnly m_DestroyedEntities;

            private NativeParallelHashSet<Entity> m_Lookup;
            private NativeList<Entity> m_DepartedEntities;

            public UpdateDepartedJob(
                NativeArray<Entity>.ReadOnly destroyedEntities,
                NativeParallelHashSet<Entity> lookup,
                NativeList<Entity> departedEntities)
            {
                m_DestroyedEntities = destroyedEntities;
                m_Lookup = lookup;
                m_DepartedEntities = departedEntities;
            }

            public void Execute()
            {
                for (int i = 0; i < m_DestroyedEntities.Length; ++i)
                {
                    Entity candidate = m_DestroyedEntities[i];
                    //If we were able to remove it from the lookup, it's departed
                    if (m_Lookup.Remove(candidate))
                    {
                        m_DepartedEntities.Add(candidate);
                    }
                }
            }
        }


        [BurstCompile]
        private struct UpdateArrivedJob : IJobEntityBatch
        {
            [ReadOnly] private readonly EntityTypeHandle m_EntityTypeHandle;
            private NativeParallelHashSet<Entity> m_Lookup;
            private NativeList<Entity> m_ArrivedEntities;

            public UpdateArrivedJob(
                EntityTypeHandle entityTypeHandle,
                NativeParallelHashSet<Entity> lookup,
                NativeList<Entity> arrivedEntities)
            {
                m_EntityTypeHandle = entityTypeHandle;
                m_Lookup = lookup;
                m_ArrivedEntities = arrivedEntities;
            }

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                NativeArray<Entity> entities = batchInChunk.GetNativeArray(m_EntityTypeHandle);

                for (int i = 0; i < entities.Length; ++i)
                {
                    Entity candidate = entities[i];
                    //If we were able to add to the lookup, it's new
                    if (m_Lookup.Add(candidate))
                    {
                        m_ArrivedEntities.Add(candidate);
                    }
                }
            }
        }
    }
}
