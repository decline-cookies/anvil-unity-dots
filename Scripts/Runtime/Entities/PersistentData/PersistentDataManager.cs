using Anvil.CSharp.Collections;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    public static class PersistentDataManager
    {
        private static readonly Dictionary<string, AbstractPersistentData> PERSISTENT_DATA_LOOKUP = new Dictionary<string, AbstractPersistentData>();
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Dispose();
        }

        public static void Dispose()
        {
            PERSISTENT_DATA_LOOKUP.DisposeAllValuesAndClear();
        }

        public static void CreatePersistentData<T>(string id,
                                                   T persistentData)
            where T : unmanaged
        {
            if (PERSISTENT_DATA_LOOKUP.ContainsKey(id))
            {
                return;
            }
            PERSISTENT_DATA_LOOKUP.Add(id, new PersistentData<T>(id, persistentData));
        }
        
        public static void CreateThreadPersistentData<T>(string id,
                                                         IThreadPersistentData<T>.ConstructionCallbackPerThread constructionCallbackPerThread,
                                                         IThreadPersistentData<T>.DisposalCallbackPerThread disposalCallbackPerThread = null)
            where T : unmanaged
        {
            if (PERSISTENT_DATA_LOOKUP.ContainsKey(id))
            {
                return;
            }
            PERSISTENT_DATA_LOOKUP.Add(id, new ThreadPersistentData<T>(id, constructionCallbackPerThread, disposalCallbackPerThread));
        }

        public static void CreateEntityPersistentData<T>(string id,
                                                         IEntityPersistentData<T>.DisposalCallbackPerEntity disposalCallbackPerEntity = null)
            where T : unmanaged
        {
            if (PERSISTENT_DATA_LOOKUP.ContainsKey(id))
            {
                return;
            }
            PERSISTENT_DATA_LOOKUP.Add(id, new EntityPersistentData<T>(id, disposalCallbackPerEntity));
        }

        public static IEntityPersistentData<T> AcquireEntityPersistentData<T>(string id, AccessType accessType)
            where T : unmanaged
        {
            EntityPersistentData<T> entityPersistentData = GetEntityPersistentData<T>(id);
            entityPersistentData.Acquire(accessType);
            return entityPersistentData;
        }
        
        public static IThreadPersistentData<T> AcquireThreadPersistentData<T>(string id, AccessType accessType)
            where T : unmanaged
        {
            ThreadPersistentData<T> threadPersistentData = GetThreadPersistentData<T>(id);
            threadPersistentData.Acquire(accessType);
            return threadPersistentData;
        }
        
        public static IPersistentData<T> AcquirePersistentData<T>(string id, AccessType accessType)
            where T : unmanaged
        {
            PersistentData<T> persistentData = GetPersistentData<T>(id);
            persistentData.Acquire(accessType);
            return persistentData;
        }


        internal static ThreadPersistentData<T> GetThreadPersistentData<T>(string id)
            where T : unmanaged
        {
            if (!PERSISTENT_DATA_LOOKUP.TryGetValue(id, out AbstractPersistentData data))
            {
                throw new InvalidOperationException($"Trying to get {nameof(ThreadPersistentData<T>)} with {nameof(id)} of {id} but it doesn't exist in the lookup! Did you call {nameof(CreateThreadPersistentData)}?");
            }

            return (ThreadPersistentData<T>)data;
        }
        
        internal static EntityPersistentData<T> GetEntityPersistentData<T>(string id)
            where T : unmanaged
        {
            if (!PERSISTENT_DATA_LOOKUP.TryGetValue(id, out AbstractPersistentData data))
            {
                throw new InvalidOperationException($"Trying to get {nameof(EntityPersistentData<T>)} with {nameof(id)} of {id} but it doesn't exist in the lookup! Did you call {nameof(CreateEntityPersistentData)}?");
            }

            return (EntityPersistentData<T>)data;
        }
        
        internal static PersistentData<T> GetPersistentData<T>(string id)
            where T : unmanaged
        {
            if (!PERSISTENT_DATA_LOOKUP.TryGetValue(id, out AbstractPersistentData data))
            {
                throw new InvalidOperationException($"Trying to get {nameof(PersistentData<T>)} with {nameof(id)} of {id} but it doesn't exist in the lookup! Did you call {nameof(CreatePersistentData)}?");
            }

            return (PersistentData<T>)data;
        }
    }
}
