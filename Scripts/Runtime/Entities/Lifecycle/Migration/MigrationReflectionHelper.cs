using Anvil.Unity.DOTS.Entities.TaskDriver;
using System;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    internal static class MigrationReflectionHelper
    {
        private static NativeParallelHashMap<long, TypeOffsetInfo> s_TypeOffsetsLookup;
        private static NativeList<TypeManager.EntityOffsetInfo> s_EntityOffsetList;
        private static NativeList<TypeManager.EntityOffsetInfo> s_BlobAssetRefOffsetList;
        private static NativeList<TypeManager.EntityOffsetInfo> s_WeakAssetRefOffsetList;

        private static readonly Type I_ENTITY_PROXY_INSTANCE = typeof(IEntityProxyInstance);

        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
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
            
            s_TypeOffsetsLookup = new NativeParallelHashMap<long, TypeOffsetInfo>(256, Allocator.Persistent);
            s_EntityOffsetList = new NativeList<TypeManager.EntityOffsetInfo>(Allocator.Persistent);
            s_BlobAssetRefOffsetList = new NativeList<TypeManager.EntityOffsetInfo>(Allocator.Persistent);
            s_WeakAssetRefOffsetList = new NativeList<TypeManager.EntityOffsetInfo>(Allocator.Persistent);


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

                    s_TypeOffsetsLookup.Add(typeHash, new TypeOffsetInfo(
                        entityOffsetStartIndex,
                        s_EntityOffsetList.Length - entityOffsetStartIndex,
                        blobOffsetStartIndex,
                        s_BlobAssetRefOffsetList.Length - blobOffsetStartIndex,
                        weakAssetStartIndex,
                        s_WeakAssetRefOffsetList.Length - weakAssetStartIndex));
                }
            }
        }

        public static void PatchEntityIfMoved<T>()
        {
            long typeHash = BurstRuntime.GetHashCode64<T>();
            TypeOffsetInfo typeOffsetInfo = s_TypeOffsetsLookup[typeHash];
            
        }



        public readonly struct TypeOffsetInfo
        {
            public readonly int EntityOffsetStartIndex;
            public readonly int EntityOffsetCount;
            public readonly int BlobAssetStartIndex;
            public readonly int BlobAssetCount;
            public readonly int WeakAssetStartIndex;
            public readonly int WeakAssetCount;

            public TypeOffsetInfo(int entityOffsetStartIndex, int entityOffsetCount, int blobAssetStartIndex, int blobAssetCount, int weakAssetStartIndex, int weakAssetCount)
            {
                EntityOffsetStartIndex = entityOffsetStartIndex;
                EntityOffsetCount = entityOffsetCount;
                BlobAssetStartIndex = blobAssetStartIndex;
                BlobAssetCount = blobAssetCount;
                WeakAssetStartIndex = weakAssetStartIndex;
                WeakAssetCount = weakAssetCount;
            }
        }
    }
}
