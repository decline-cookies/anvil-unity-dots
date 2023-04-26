using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class EntityProxyDataSource<TInstance> : AbstractDataSource<EntityProxyInstanceWrapper<TInstance>>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private EntityProxyDataSourceConsolidator<TInstance> m_Consolidator;

        public EntityProxyDataSource(TaskDriverManagementSystem taskDriverManagementSystem) : base(taskDriverManagementSystem) { }

        protected override void DisposeSelf()
        {
            m_Consolidator.Dispose();
            base.DisposeSelf();
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
        // MIGRATION
        //*************************************************************************************************************
        
        public override void MigrateTo(
            IDataSource destinationDataSource, 
            ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray,
            ref NativeParallelHashMap<uint, uint> taskSetOwnerIDMapping,
            ref NativeParallelHashMap<uint, uint> activeIDMapping)
        {
            EntityProxyDataSource<TInstance> destination = destinationDataSource as EntityProxyDataSource<TInstance>;
            
            PendingData.Acquire(AccessType.ExclusiveWrite);

            UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> stream = PendingData.Pending;
            NativeArray<EntityProxyInstanceWrapper<TInstance>> instanceArray = stream.ToNativeArray(Allocator.Temp);
            stream.Clear();

            for (int i = 0; i < instanceArray.Length; ++i)
            {
                //If we didn't move, we need to rewrite to the stream since we didn't move
                EntityProxyInstanceWrapper<TInstance> instance = instanceArray[i];
                EntityProxyInstanceID instanceID = instance.InstanceID;

                //See what our entity was remapped to
                Entity remappedEntity = EntityRemapUtility.RemapEntity(ref remapArray, instanceID.Entity);
                //If we were remapped to null, then we don't exist in the new world, we should just stay here
                if (remappedEntity == Entity.Null)
                {
                    //TODO: Write back to the original stream, we stayed in this world
                    continue;
                }
                
                //If we don't have a destination in the new world, then we can just let these cease to exist
                if (destination == null)
                {
                    continue;
                }
                
                //If we do have a destination, then we will want to patch the entity references
                instance.PatchEntityReferences(remappedEntity);
                
                //Rewrite the memory for the TaskSetOwnerID and ActiveID
                instance.PatchIDs(
                    taskSetOwnerIDMapping[instanceID.TaskSetOwnerID],
                    activeIDMapping[instanceID.ActiveID]);
                
                //TODO: Write to the new stream
                
            }

            PendingData.Release();
        }


        public unsafe void Migrate(NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
        {
            PendingData.Acquire(AccessType.ExclusiveWrite);
            //TODO: Need to ensure this is actually the ref - look at NativeParallelHashMap
            foreach (EntityProxyInstanceWrapper<TInstance> entry in PendingData.Pending)
            {
                EntityRemapUtility.CalculateFieldOffsetsUnmanaged(
                    typeof(EntityProxyInstanceWrapper<TInstance>),
                    out bool hasEntityRefs,
                    out bool hasBlobRefs,
                    out bool hasWeakAssetRefs,
                    ref s_EntityOffsetList,
                    ref s_BlobAssetRefOffsetList,
                    ref s_WeakAssetRefOffsetList);

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
