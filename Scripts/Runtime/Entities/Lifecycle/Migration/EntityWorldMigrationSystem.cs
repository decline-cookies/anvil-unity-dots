using Anvil.CSharp.Logging;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// World specific system for handling Migration.
    /// Register <see cref="IEntityWorldMigrationObserver"/>s here to be notified when Migration occurs
    ///
    /// NOTE: Use <see cref="MoveEntitiesAndMigratableDataTo"/> on this System instead of directly interfacing with
    /// <see cref="EntityManager.MoveEntitiesFrom"/>
    /// </summary>
    public class EntityWorldMigrationSystem : AbstractDataSystem
    {
        private readonly HashSet<IEntityWorldMigrationObserver> m_MigrationObservers;

        // ReSharper disable once InconsistentNaming
        private NativeList<JobHandle> m_Dependencies_ScratchPad;

        public EntityWorldMigrationSystem()
        {
            m_MigrationObservers = new HashSet<IEntityWorldMigrationObserver>();
            m_Dependencies_ScratchPad = new NativeList<JobHandle>(8, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            m_Dependencies_ScratchPad.Dispose();
            base.OnDestroy();
        }

        /// <summary>
        /// Adds a <see cref="IEntityWorldMigrationObserver"/> to be notified when Migration occurs and be given the chance to
        /// respond to it.
        /// </summary>
        /// <param name="entityWorldMigrationObserver">The <see cref="IEntityWorldMigrationObserver"/></param>
        public void RegisterMigrationObserver(IEntityWorldMigrationObserver entityWorldMigrationObserver)
        {
            m_MigrationObservers.Add(entityWorldMigrationObserver);
            m_Dependencies_ScratchPad.ResizeUninitialized(m_MigrationObservers.Count);
        }

        /// <summary>
        /// Removes a <see cref="IEntityWorldMigrationObserver"/> if it no longer wishes to be notified of when a Migration occurs.
        /// </summary>
        /// <param name="entityWorldMigrationObserver">The <see cref="IEntityWorldMigrationObserver"/></param>
        public void UnregisterMigrationObserver(IEntityWorldMigrationObserver entityWorldMigrationObserver)
        {
            //We've already been destroyed, no need to unregister
            if (!m_Dependencies_ScratchPad.IsCreated)
            {
                return;
            }
            m_MigrationObservers.Remove(entityWorldMigrationObserver);
            m_Dependencies_ScratchPad.ResizeUninitialized(m_MigrationObservers.Count);
        }

        /// <summary>
        /// Migrates Entities from this <see cref="World"/> to the destination world with the provided query.
        /// This will then handle notifying all <see cref="IEntityWorldMigrationObserver"/>s to have the chance to respond with
        /// custom migration work.
        /// </summary>
        /// <param name="destinationWorld">The <see cref="World"/> to move Entities to.</param>
        /// <param name="entitiesToMigrateQuery">The <see cref="EntityQuery"/> to select the Entities to migrate.</param>
        public void MoveEntitiesAndMigratableDataTo(World destinationWorld, EntityQuery entitiesToMigrateQuery)
        {
            NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray = EntityManager.CreateEntityRemapArray(Allocator.TempJob);
            //Do the actual move and get back the remap info
            destinationWorld.EntityManager.MoveEntitiesFrom(EntityManager, entitiesToMigrateQuery, remapArray);

            //Let everyone have a chance to do any additional remapping
            JobHandle dependsOn = NotifyObserversOfMigrateTo(destinationWorld, ref remapArray);
            //Dispose the array based on those remapping jobs being complete
            remapArray.Dispose(dependsOn);
            //Immediately complete the jobs so migration is complete and the world's state is correct
            dependsOn.Complete();
        }

        private JobHandle NotifyObserversOfMigrateTo(World destinationWorld, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
        {
            int index = 0;
            foreach (IEntityWorldMigrationObserver migrationObserver in m_MigrationObservers)
            {
                m_Dependencies_ScratchPad[index] = migrationObserver.MigrateTo(default, destinationWorld, ref remapArray);
                index++;
            }
            return JobHandle.CombineDependencies(m_Dependencies_ScratchPad.AsArray());
        }

        //*************************************************************************************************************
        // STATIC REGISTRATION
        //*************************************************************************************************************

        private static UnsafeParallelHashMap<long, TypeOffsetInfo> s_TypeOffsetsLookup = new UnsafeParallelHashMap<long, TypeOffsetInfo>(256, Allocator.Persistent);
        private static NativeList<TypeManager.EntityOffsetInfo> s_EntityOffsetList = new NativeList<TypeManager.EntityOffsetInfo>(32, Allocator.Persistent);
        private static NativeList<TypeManager.EntityOffsetInfo> s_BlobAssetRefOffsetList = new NativeList<TypeManager.EntityOffsetInfo>(32, Allocator.Persistent);
        private static NativeList<TypeManager.EntityOffsetInfo> s_WeakAssetRefOffsetList = new NativeList<TypeManager.EntityOffsetInfo>(32, Allocator.Persistent);

        private static bool s_AppDomainUnloadRegistered;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            //This pattern ensures we can setup and dispose properly the static native collections without Unity
            //getting upset about memory leaks
            if (s_AppDomainUnloadRegistered)
            {
                return;
            }
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_OnDomainUnload;
            s_AppDomainUnloadRegistered = true;

            SharedTypeOffsetInfo.REF.Data = s_TypeOffsetsLookup;
            UpdateSharedStatics();
        }

        private static void CurrentDomain_OnDomainUnload(object sender, EventArgs e)
        {
            SharedTypeOffsetInfo.REF.Data = default;
            SharedEntityOffsetInfo.REF.Data = default;
            SharedBlobAssetRefInfo.REF.Data = default;
            SharedWeakAssetRefInfo.REF.Data = default;

            if (s_TypeOffsetsLookup.IsCreated)
            {
                s_TypeOffsetsLookup.Dispose();
            }
            if (s_EntityOffsetList.IsCreated)
            {
                s_EntityOffsetList.Dispose();
            }
            if (s_BlobAssetRefOffsetList.IsCreated)
            {
                s_BlobAssetRefOffsetList.Dispose();
            }
            if (s_WeakAssetRefOffsetList.IsCreated)
            {
                s_WeakAssetRefOffsetList.Dispose();
            }
        }

        private static unsafe void UpdateSharedStatics()
        {
            SharedEntityOffsetInfo.REF.Data = new IntPtr(s_EntityOffsetList.GetUnsafePtr());
            SharedBlobAssetRefInfo.REF.Data = new IntPtr(s_BlobAssetRefOffsetList.GetUnsafePtr());
            SharedWeakAssetRefInfo.REF.Data = new IntPtr(s_WeakAssetRefOffsetList.GetUnsafePtr());
        }

        /// <summary>
        /// Registers the Type that may contain Entity references so that it can be used with
        /// <see cref="EntityWorldMigrationExtension.PatchEntityReferences{T}"/> to remap Entity references.
        /// </summary>
        /// <typeparam name="T">The type to register</typeparam>
        public static void RegisterForEntityPatching<T>()
            where T : struct
        {
            RegisterForEntityPatching(typeof(T));
        }

        /// <inheritdoc cref="RegisterForEntityPatching{T}"/>
        /// <exception cref="InvalidOperationException">
        /// Occurs when the Type is not a Value type.
        /// </exception>
        public static void RegisterForEntityPatching(Type type)
        {
            if (!type.IsValueType)
            {
                throw new InvalidOperationException($"Type {type.GetReadableName()} must be a value type in order to register for Entity Patching.");
            }

            long typeHash = BurstRuntime.GetHashCode64(type);
            //We've already added this type, no need to do so again
            if (s_TypeOffsetsLookup.ContainsKey(typeHash))
            {
                return;
            }

            int entityOffsetStartIndex = s_EntityOffsetList.Length;

            //We'll allow for a TypeOffset to be registered even if there's nothing to remap so that it's easy to detect
            //when you forgot to register a type. We'll ignore the bools that this function returns.
            EntityRemapUtility.CalculateFieldOffsetsUnmanaged(
                type,
                out bool hasEntityRefs,
                out bool hasBlobRefs,
                out bool hasWeakAssetRefs,
                ref s_EntityOffsetList,
                ref s_BlobAssetRefOffsetList,
                ref s_WeakAssetRefOffsetList);


            //Unity gives us back Blob Asset Refs and Weak Asset Refs as well but for now we're ignoring them.
            //When the time comes to use those and do remapping with them, we'll need to add that info here along 
            //with the utils to actually do the remapping
            s_TypeOffsetsLookup.Add(
                typeHash,
                new TypeOffsetInfo(
                    entityOffsetStartIndex,
                    s_EntityOffsetList.Length));

            //The size of the underlying data could have changed such that we re-allocated the memory, so we'll update
            //our shared statics
            UpdateSharedStatics();
        }

        //*************************************************************************************************************
        // HELPER TYPES
        //*************************************************************************************************************

        internal readonly struct TypeOffsetInfo
        {
            public readonly int EntityOffsetStartIndex;
            public readonly int EntityOffsetEndIndex;

            public bool CanRemap
            {
                get => EntityOffsetEndIndex > EntityOffsetStartIndex;
            }

            public TypeOffsetInfo(int entityOffsetStartIndex, int entityOffsetEndIndex)
            {
                EntityOffsetStartIndex = entityOffsetStartIndex;
                EntityOffsetEndIndex = entityOffsetEndIndex;
            }
        }


        //*************************************************************************************************************
        // SHARED STATIC REQUIREMENTS
        //*************************************************************************************************************

        // ReSharper disable once ConvertToStaticClass
        // ReSharper disable once ClassNeverInstantiated.Local
        private sealed class MigrationUtilContext
        {
            private MigrationUtilContext() { }
        }

        internal sealed class SharedTypeOffsetInfo
        {
            public static readonly SharedStatic<UnsafeParallelHashMap<long, TypeOffsetInfo>> REF = SharedStatic<UnsafeParallelHashMap<long, TypeOffsetInfo>>.GetOrCreate<MigrationUtilContext, SharedTypeOffsetInfo>();
        }

        internal sealed class SharedEntityOffsetInfo
        {
            public static readonly SharedStatic<IntPtr> REF = SharedStatic<IntPtr>.GetOrCreate<MigrationUtilContext, SharedEntityOffsetInfo>();
        }

        internal sealed class SharedBlobAssetRefInfo
        {
            public static readonly SharedStatic<IntPtr> REF = SharedStatic<IntPtr>.GetOrCreate<MigrationUtilContext, SharedBlobAssetRefInfo>();
        }

        internal sealed class SharedWeakAssetRefInfo
        {
            public static readonly SharedStatic<IntPtr> REF = SharedStatic<IntPtr>.GetOrCreate<MigrationUtilContext, SharedWeakAssetRefInfo>();
        }
    }
}
