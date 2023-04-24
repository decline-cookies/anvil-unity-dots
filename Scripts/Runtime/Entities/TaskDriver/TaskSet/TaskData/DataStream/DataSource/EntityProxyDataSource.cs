using Anvil.Unity.DOTS.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class EntityProxyDataSource<TInstance> : AbstractDataSource<EntityProxyInstanceWrapper<TInstance>>,
                                                      IMigratable
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private static NativeList<TypeManager.EntityOffsetInfo> s_EntityOffsetList = new NativeList<TypeManager.EntityOffsetInfo>(Allocator.Persistent);
        private static NativeList<TypeManager.EntityOffsetInfo> s_BlobAssetRefOffsetList = new NativeList<TypeManager.EntityOffsetInfo>(Allocator.Persistent);
        private static NativeList<TypeManager.EntityOffsetInfo> s_WeakAssetRefOffsetList = new NativeList<TypeManager.EntityOffsetInfo>(Allocator.Persistent);
        
        private EntityProxyDataSourceConsolidator<TInstance> m_Consolidator;

        public EntityProxyDataSource(TaskDriverManagementSystem taskDriverManagementSystem) : base(taskDriverManagementSystem) { }

        protected override void DisposeSelf()
        {
            m_Consolidator.Dispose();
            base.DisposeSelf();
        }

        public unsafe void Migrate(NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
        {

            PendingData.Acquire(AccessType.ExclusiveWrite);
            //TODO: Need to ensure this is actually the ref - look at NativeParallelHashMap
            foreach (EntityProxyInstanceWrapper<TInstance> entry in PendingData.Pending)
            {
                Entity entity = entry.InstanceID.Entity;
                Entity remapEntity = EntityRemapUtility.RemapEntity(ref remapArray, entity);
                
                EntityRemapUtility.HasEntityReferencesManaged(typeof(EntityProxyInstanceWrapper<TInstance>), out bool hasEntityReferences, out bool hasBlobReferences);
                EntityRemapUtility.CalculateFieldOffsetsUnmanaged(typeof(EntityProxyInstanceWrapper<TInstance>), out bool hasEntityRefs, out bool hasBlobRefs,
                    out bool hasWeakAssetRefs, ref s_EntityOffsetList, ref s_BlobAssetRefOffsetList, ref s_WeakAssetRefOffsetList);

                EntityProxyInstanceWrapper<TInstance> copy = entry;
                byte* copyPtr = (byte*)UnsafeUtility.AddressOf(ref copy);
                void* startCopyPtr = copyPtr;
                for (int i = 0; i < s_EntityOffsetList.Length; ++i)
                {
                    TypeManager.EntityOffsetInfo offsetInfo = s_EntityOffsetList[i];
                    copyPtr += offsetInfo.Offset;
                    Entity* offsetEntity = (Entity*)copyPtr;
                    *offsetEntity = EntityRemapUtility.RemapEntity(ref remapArray, *offsetEntity);
                }

                

                copy = *(EntityProxyInstanceWrapper<TInstance>*)startCopyPtr;

                float a = 5.0f;

            }
            PendingData.Release();
        }

        protected override void HardenSelf()
        {
            base.HardenSelf();

            //We need to ensure we get the right access to any of the cancel data structures
            foreach (AbstractData data in ActiveDataLookupByID.Values)
            {
                //If this piece of data can be cancelled, we need to be able to read the associated Cancel Request lookup
                if (data.CancelRequestBehaviour is CancelRequestBehaviour.Delete or CancelRequestBehaviour.Unwind)
                {
                    AddConsolidationData(data.TaskSetOwner.TaskSet.CancelRequestsDataStream.ActiveLookupData, AccessType.SharedRead);
                }
            }

            m_Consolidator = new EntityProxyDataSourceConsolidator<TInstance>(PendingData, ActiveDataLookupByID);
        }

        //*************************************************************************************************************
        // EXECUTION
        //*************************************************************************************************************
        protected sealed override JobHandle ConsolidateSelf(JobHandle dependsOn)
        {
            ConsolidateEntityProxyDataSourceJob consolidateEntityProxyDataSourceJob = new ConsolidateEntityProxyDataSourceJob(m_Consolidator);
            dependsOn = consolidateEntityProxyDataSourceJob.Schedule(dependsOn);

            return dependsOn;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ConsolidateEntityProxyDataSourceJob : IJob
        {
            private EntityProxyDataSourceConsolidator<TInstance> m_Consolidator;

            public ConsolidateEntityProxyDataSourceJob(EntityProxyDataSourceConsolidator<TInstance> dataSourceConsolidator) : this()
            {
                m_Consolidator = dataSourceConsolidator;
            }

            public void Execute()
            {
                m_Consolidator.Consolidate();
            }
        }
    }
}
