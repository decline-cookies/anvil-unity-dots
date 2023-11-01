using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Helper class for when Entities are migrating from one <see cref="World"/> to another.
    /// </summary>
    public static class EntityWorldMigrationExtension
    {
        //*************************************************************************************************************
        // MIGRATION
        //*************************************************************************************************************

        /// <summary>
        /// Migrates Entities from this <see cref="World"/> to the destination world with the provided query.
        /// This will then handle notifying all <see cref="IEntityWorldMigrationObserver"/>s to have the chance to respond with
        /// custom migration work.
        /// NOTE: Use this instead of <see cref="EntityManager.MoveEntitiesFrom"/> in order for migration callbacks
        /// to occur in non IComponentData 
        /// </summary>
        /// <param name="srcEntityManager">The source <see cref="World"/>'s Entity Manager</param>
        /// <param name="destinationWorld">The <see cref="World"/> to move Entities to.</param>
        /// <param name="entitiesToMigrateQuery">The <see cref="EntityQuery"/> to select the Entities to migrate.</param>
        public static void MoveEntitiesAndMigratableDataTo(this EntityManager srcEntityManager, World destinationWorld, EntityQuery entitiesToMigrateQuery)
        {
            EntityWorldMigrationSystem entityWorldMigrationSystem = srcEntityManager.World.GetOrCreateSystemManaged<EntityWorldMigrationSystem>();
            entityWorldMigrationSystem.MoveEntitiesAndMigratableDataTo(destinationWorld, entitiesToMigrateQuery);
        }

        //*************************************************************************************************************
        // BURST RUNTIME CALLS
        //*************************************************************************************************************

        /// <summary>
        /// Checks if the Entity was remapped by Unity during a world transfer.
        /// </summary>
        /// <param name="currentEntity">The current entity in this World</param>
        /// <param name="remapArray">The remap array Unity provided.</param>
        /// <param name="remappedEntity">The remapped Entity in the new World if it exists.</param>
        /// <returns>
        /// true if this entity was moved to the new world and remaps to a new entity.
        /// false if this entity did not move and stayed in this world.
        /// </returns>
        [GenerateTestsForBurstCompatibility]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetRemappedEntity(
            this Entity currentEntity,
            ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray,
            out Entity remappedEntity)
        {
            remappedEntity = EntityRemapUtility.RemapEntity(ref remapArray, currentEntity);
            return remappedEntity != Entity.Null;
        }

        /// <summary>
        /// For a given struct and Unity provided remapping array, all <see cref="Entity"/> references will be
        /// remapped to the new entity reference in the new world.
        /// Entities that remained in this world will not be remapped.
        /// </summary>
        /// <param name="instance">The struct to patch</param>
        /// <param name="remapArray">The Unity provided remap array</param>
        /// <typeparam name="T">The type of struct to patch</typeparam>
        /// <exception cref="InvalidOperationException">
        /// Occurs if this type was not registered via <see cref="EntityWorldMigrationSystem.RegisterForEntityPatching{T}"/>
        /// </exception>
        [GenerateTestsForBurstCompatibility]
        public static unsafe void PatchEntityReferences<T>(this ref T instance, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
            where T : struct
        {
            long typeHash = BurstRuntime.GetHashCode64<T>();
            //Easy way to check if we remembered to register our type. Unfortunately it's a lot harder to figure out which type is missing due to the hash
            //but usually you're going to run into this right away and be able to figure it out. Not using the actual Type class so we can Burst this.
            if (!EntityWorldMigrationSystem.SharedTypeOffsetInfo.REF.Data.TryGetValue(typeHash, out EntityWorldMigrationSystem.TypeOffsetInfo typeOffsetInfo))
            {
                throw new InvalidOperationException($"Tried to patch type with BurstRuntime hash of {typeHash} but it wasn't registered. Did you call {nameof(EntityWorldMigrationSystem.RegisterForEntityPatching)}?");
            }

            //If there's nothing to remap, we'll just return
            if (!typeOffsetInfo.CanRemap)
            {
                return;
            }

            //Otherwise we'll get the memory address of the instance and run through all possible entity references
            //to remap to the new entity
            byte* instancePtr = (byte*)UnsafeUtility.AddressOf(ref instance);
            //Beginning of the list
            TypeManager.EntityOffsetInfo* entityOffsetInfoPtr = (TypeManager.EntityOffsetInfo*)EntityWorldMigrationSystem.SharedEntityOffsetInfo.REF.Data;
            for (int i = typeOffsetInfo.EntityOffsetStartIndex; i < typeOffsetInfo.EntityOffsetEndIndex; ++i)
            {
                //Index into the list
                TypeManager.EntityOffsetInfo* entityOffsetInfo = entityOffsetInfoPtr + i;
                //Get offset info from list and offset into the instance memory
                Entity* entityPtr = (Entity*)(instancePtr + entityOffsetInfo->Offset);
                //Patch
                *entityPtr = EntityRemapUtility.RemapEntity(ref remapArray, *entityPtr);
            }
        }
    }
}
