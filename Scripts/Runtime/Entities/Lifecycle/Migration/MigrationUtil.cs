using Anvil.CSharp.Logging;
using System;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Helper class for when Entities are migrating from one <see cref="World"/> to another.
    /// </summary>
    public static class MigrationUtil
    {
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

        /// <summary>
        /// Registers the Type that may contain Entity references so that it can be used with
        /// <see cref="PatchEntityReferences{T}"/> to remap Entity references.
        /// </summary>
        /// <typeparam name="T">The type to register</typeparam>
        public static void RegisterTypeForEntityPatching<T>()
            where T : struct
        {
            RegisterTypeForEntityPatching(typeof(T));
        }

        /// <inheritdoc cref="RegisterTypeForEntityPatching{T}"/>
        /// <exception cref="InvalidOperationException">
        /// Occurs when the Type is not a Value type.
        /// </exception>
        public static void RegisterTypeForEntityPatching(Type type)
        {
            if (!type.IsValueType)
            {
                throw new InvalidOperationException($"Type {type.GetReadableName()} must be a value type in order to register for Entity Patching.");
            }

            ScanForCollections(string.Empty, type);

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

        private static void ScanForCollections(string parentPathString, Type type)
        {
            //One of these fields might be a collection or contain a collection, best way to tell is to scan it and see if it has a pointer
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in fields)
            {
                Type fieldType = field.FieldType;
                if (fieldType.IsPrimitive)
                {
                    continue;
                }
                if (fieldType.IsPointer)
                {
                    Debug.LogWarning($"{parentPathString}/{type.GetReadableName()} has a field named {field.Name} which is a pointer. This is probably a collection. As a result, we cannot automatically patch any entity references inside this collection. It would need to be handled manually until automatic work can handle in from https://github.com/decline-cookies/anvil-unity-dots/issues/233.");
                    continue;
                }
                ScanForCollections($"{parentPathString}/{type.GetReadableName()}", fieldType);
            }
        }

        private static unsafe void UpdateSharedStatics()
        {
            SharedEntityOffsetInfo.REF.Data = new IntPtr(s_EntityOffsetList.GetUnsafePtr());
            SharedBlobAssetRefInfo.REF.Data = new IntPtr(s_BlobAssetRefOffsetList.GetUnsafePtr());
            SharedWeakAssetRefInfo.REF.Data = new IntPtr(s_WeakAssetRefOffsetList.GetUnsafePtr());
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
        [BurstCompatible]
        public static bool IfEntityIsRemapped(
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
        /// Occurs if this type was not registered via <see cref="RegisterTypeForEntityPatching{T}"/>
        /// </exception>
        [BurstCompatible]
        public static unsafe void PatchEntityReferences<T>(this ref T instance, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
            where T : struct
        {
            long typeHash = BurstRuntime.GetHashCode64<T>();
            //Easy way to check if we remembered to register our type. Unfortunately it's a lot harder to figure out which type is missing due to the hash
            //but usually you're going to run into this right away and be able to figure it out. Not using the actual Type class so we can Burst this.
            if (!SharedTypeOffsetInfo.REF.Data.TryGetValue(typeHash, out TypeOffsetInfo typeOffsetInfo))
            {
                throw new InvalidOperationException($"Tried to patch type with BurstRuntime hash of {typeHash} but it wasn't registered. Did you call {nameof(RegisterTypeForEntityPatching)}?");
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
            TypeManager.EntityOffsetInfo* entityOffsetInfoPtr = (TypeManager.EntityOffsetInfo*)SharedEntityOffsetInfo.REF.Data;
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

        //*************************************************************************************************************
        // HELPER TYPES
        //*************************************************************************************************************

        private readonly struct TypeOffsetInfo
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

        private sealed class SharedTypeOffsetInfo
        {
            public static readonly SharedStatic<UnsafeParallelHashMap<long, TypeOffsetInfo>> REF = SharedStatic<UnsafeParallelHashMap<long, TypeOffsetInfo>>.GetOrCreate<MigrationUtilContext, SharedTypeOffsetInfo>();
        }

        private sealed class SharedEntityOffsetInfo
        {
            public static readonly SharedStatic<IntPtr> REF = SharedStatic<IntPtr>.GetOrCreate<MigrationUtilContext, SharedEntityOffsetInfo>();
        }

        private sealed class SharedBlobAssetRefInfo
        {
            public static readonly SharedStatic<IntPtr> REF = SharedStatic<IntPtr>.GetOrCreate<MigrationUtilContext, SharedBlobAssetRefInfo>();
        }

        private sealed class SharedWeakAssetRefInfo
        {
            public static readonly SharedStatic<IntPtr> REF = SharedStatic<IntPtr>.GetOrCreate<MigrationUtilContext, SharedWeakAssetRefInfo>();
        }
    }
}
