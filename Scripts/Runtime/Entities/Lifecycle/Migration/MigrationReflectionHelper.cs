using Anvil.Unity.DOTS.Entities.TaskDriver;
using System;
using System.Reflection;
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

        private static readonly Type I_ENTITY_PROXY_INSTANCE = typeof(IEntityProxyInstance);

        private static bool s_AppDomainUnloadRegistered;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            if (!s_AppDomainUnloadRegistered)
            {
                AppDomain.CurrentDomain.DomainUnload += CurrentDomain_OnDomainUnload;
                s_AppDomainUnloadRegistered = true;
            }


            Type genericWrapperType = typeof(EntityProxyInstanceWrapper<>);

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                {
                    return;
                }
                foreach (Type type in assembly.GetTypes())
                {
                    if (!type.IsValueType || !I_ENTITY_PROXY_INSTANCE.IsAssignableFrom(type))
                    {
                        continue;
                    }

                    Type concreteType = genericWrapperType.MakeGenericType(type);
                    long typeHash = BurstRuntime.GetHashCode64(concreteType);

                    int entityOffsetStartIndex = s_EntityOffsetList.Length;
                    int blobOffsetStartIndex = s_BlobAssetRefOffsetList.Length;
                    int weakAssetStartIndex = s_WeakAssetRefOffsetList.Length;

                    EntityRemapUtility.CalculateFieldOffsetsUnmanaged(
                        concreteType,
                        out bool hasEntityRefs,
                        out bool hasBlobRefs,
                        out bool hasWeakAssetRefs,
                        ref s_EntityOffsetList,
                        ref s_BlobAssetRefOffsetList,
                        ref s_WeakAssetRefOffsetList);

                    if (!hasEntityRefs && !hasBlobRefs && !hasWeakAssetRefs)
                    {
                        continue;
                    }

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
            }
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
            where T : unmanaged
        {
            long typeHash = BurstRuntime.GetHashCode64<T>();
            TypeOffsetInfo typeOffsetInfo = s_TypeOffsetsLookup[typeHash];

            byte* instancePtr = (byte*)UnsafeUtility.AddressOf(ref instance);
            for (int i = typeOffsetInfo.EntityOffsetStartIndex; i < typeOffsetInfo.EntityOffsetEndIndex; ++i)
            {
                TypeManager.EntityOffsetInfo entityOffsetInfo = s_EntityOffsetList[i];
                Entity* entityPtr = (Entity*)(instancePtr + entityOffsetInfo.Offset);
                *entityPtr = remappedEntity;
            }
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
