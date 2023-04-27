using Anvil.CSharp.Logging;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    internal static class MigrationReflectionHelper
    {
        private static NativeParallelHashMap<long, TypeOffsetInfo> s_TypeOffsetsLookup = new NativeParallelHashMap<long, TypeOffsetInfo>(256, Allocator.Persistent);
        private static NativeList<TypeManager.EntityOffsetInfo> s_EntityOffsetList = new NativeList<TypeManager.EntityOffsetInfo>(Allocator.Persistent);
        private static NativeList<TypeManager.EntityOffsetInfo> s_BlobAssetRefOffsetList = new NativeList<TypeManager.EntityOffsetInfo>(Allocator.Persistent);
        private static NativeList<TypeManager.EntityOffsetInfo> s_WeakAssetRefOffsetList = new NativeList<TypeManager.EntityOffsetInfo>(Allocator.Persistent);

        private static bool s_AppDomainUnloadRegistered;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            if (!s_AppDomainUnloadRegistered)
            {
                AppDomain.CurrentDomain.DomainUnload += CurrentDomain_OnDomainUnload;
                s_AppDomainUnloadRegistered = true;
            }
        }

        public static void RegisterTypeForEntityPatching<T>()
            where T : struct
        {
            RegisterTypeForEntityPatching(typeof(T));
        }

        public static void RegisterTypeForEntityPatching(Type type)
        {
            if (!type.IsValueType)
            {
                throw new InvalidOperationException($"Type {type.GetReadableName()} must be a value type in order to register for Entity Patching.");
            }

            long typeHash = BurstRuntime.GetHashCode64(type);
            //We've already added this type
            if (s_TypeOffsetsLookup.ContainsKey(typeHash))
            {
                return;
            }

            int entityOffsetStartIndex = s_EntityOffsetList.Length;
            int blobOffsetStartIndex = s_BlobAssetRefOffsetList.Length;
            int weakAssetStartIndex = s_WeakAssetRefOffsetList.Length;

            EntityRemapUtility.CalculateFieldOffsetsUnmanaged(
                type,
                out bool hasEntityRefs,
                out bool hasBlobRefs,
                out bool hasWeakAssetRefs,
                ref s_EntityOffsetList,
                ref s_BlobAssetRefOffsetList,
                ref s_WeakAssetRefOffsetList);

            //We'll allow for a TypeOffset to be registered even if there's nothing to remap so that it's easy to detect
            //when you forgot to register a type.

            s_TypeOffsetsLookup.Add(
                typeHash,
                new TypeOffsetInfo(
                    entityOffsetStartIndex,
                    s_EntityOffsetList.Length,
                    blobOffsetStartIndex,
                    s_BlobAssetRefOffsetList.Length,
                    weakAssetStartIndex,
                    s_WeakAssetRefOffsetList.Length));
        }

        private static void CurrentDomain_OnDomainUnload(object sender, EventArgs e)
        {
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


        public static unsafe void PatchEntityReferences<T>(this ref T instance, ref Entity remappedEntity)
            where T : struct
        {
            long typeHash = BurstRuntime.GetHashCode64<T>();
            if (!s_TypeOffsetsLookup.TryGetValue(typeHash, out TypeOffsetInfo typeOffsetInfo))
            {
                throw new InvalidOperationException($"Tried to patch type with BurstRuntime hash of {typeHash} but it wasn't registered. Did you call {nameof(RegisterTypeForEntityPatching)}?");
            }

            byte* instancePtr = (byte*)UnsafeUtility.AddressOf(ref instance);
            for (int i = typeOffsetInfo.EntityOffsetStartIndex; i < typeOffsetInfo.EntityOffsetEndIndex; ++i)
            {
                TypeManager.EntityOffsetInfo entityOffsetInfo = s_EntityOffsetList[i];
                Entity* entityPtr = (Entity*)(instancePtr + entityOffsetInfo.Offset);
                *entityPtr = remappedEntity;
            }

            //TODO: Patch for Blobs and Weaks?
        }


        public readonly struct TypeOffsetInfo
        {
            public readonly int EntityOffsetStartIndex;
            public readonly int EntityOffsetEndIndex;
            public readonly int BlobAssetStartIndex;
            public readonly int BlobAssetEndIndex;
            public readonly int WeakAssetStartIndex;
            public readonly int WeakAssetEndIndex;

            public TypeOffsetInfo(int entityOffsetStartIndex, int entityOffsetEndIndex, int blobAssetStartIndex, int blobAssetEndIndex, int weakAssetStartIndex, int weakAssetEndIndex)
            {
                EntityOffsetStartIndex = entityOffsetStartIndex;
                EntityOffsetEndIndex = entityOffsetEndIndex;
                BlobAssetStartIndex = blobAssetStartIndex;
                BlobAssetEndIndex = blobAssetEndIndex;
                WeakAssetStartIndex = weakAssetStartIndex;
                WeakAssetEndIndex = weakAssetEndIndex;
            }
        }
    }
}
