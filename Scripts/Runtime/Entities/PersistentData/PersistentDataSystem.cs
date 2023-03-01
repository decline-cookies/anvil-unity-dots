using Anvil.CSharp.Collections;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities
{
    public partial class PersistentDataSystem : AbstractAnvilSystemBase
    {
        private static readonly Dictionary<string, AbstractPersistentData> s_ThreadLookup = new Dictionary<string, AbstractPersistentData>();
        private static int s_InstanceCount;
        
        private readonly Dictionary<string, AbstractPersistentData> m_Lookup;

        public PersistentDataSystem()
        {
            s_InstanceCount++;
            m_Lookup = new Dictionary<string, AbstractPersistentData>();
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
        }

        protected override void OnDestroy()
        {
            m_Lookup.DisposeAllValuesAndClear();
            s_InstanceCount--;
            if (s_InstanceCount <= 0)
            {
                s_ThreadLookup.DisposeAllValuesAndClear();
            }
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            //Just in case
            Enabled = false;
        }


        public void CreatePersistentData<T>(string id, T persistentData) where T : unmanaged
        {
            if (m_Lookup.ContainsKey(id))
            {
                return;
            }

            m_Lookup.Add(id, new PersistentData<T>(id, persistentData));
        }

        public void CreateThreadPersistentData<T>(
            string id,
            IThreadPersistentData<T>.ConstructionCallbackPerThread constructionCallbackPerThread,
            IThreadPersistentData<T>.DisposalCallbackPerThread disposalCallbackPerThread = null)
            where T : unmanaged
        {
            if (s_ThreadLookup.ContainsKey(id))
            {
                return;
            }

            s_ThreadLookup.Add(id, new ThreadPersistentData<T>(id, constructionCallbackPerThread, disposalCallbackPerThread));
        }

        public void CreateEntityPersistentData<T>(
            string id,
            IEntityPersistentData<T>.DisposalCallbackPerEntity disposalCallbackPerEntity = null)
            where T : unmanaged
        {
            if (m_Lookup.ContainsKey(id))
            {
                return;
            }

            m_Lookup.Add(id, new EntityPersistentData<T>(id, disposalCallbackPerEntity));
        }

        public IEntityPersistentData<T> AcquireEntityPersistentData<T>(string id, AccessType accessType)
            where T : unmanaged
        {
            EntityPersistentData<T> entityPersistentData = GetEntityPersistentData<T>(id);
            entityPersistentData.Acquire(accessType);

            return entityPersistentData;
        }

        public IThreadPersistentData<T> AcquireThreadPersistentData<T>(string id, AccessType accessType)
            where T : unmanaged
        {
            ThreadPersistentData<T> threadPersistentData = GetThreadPersistentData<T>(id);
            threadPersistentData.Acquire(accessType);

            return threadPersistentData;
        }

        public IPersistentData<T> AcquirePersistentData<T>(string id, AccessType accessType) where T : unmanaged
        {
            PersistentData<T> persistentData = GetPersistentData<T>(id);
            persistentData.Acquire(accessType);

            return persistentData;
        }


        internal ThreadPersistentData<T> GetThreadPersistentData<T>(string id) where T : unmanaged
        {
            if (!s_ThreadLookup.TryGetValue(id, out AbstractPersistentData data))
            {
                throw new InvalidOperationException($"Trying to get {nameof(ThreadPersistentData<T>)} with {nameof(id)} of {id} but it doesn't exist in the lookup! Did you call {nameof(CreateThreadPersistentData)}?");
            }

            return (ThreadPersistentData<T>)data;
        }

        internal EntityPersistentData<T> GetEntityPersistentData<T>(string id) where T : unmanaged
        {
            if (!m_Lookup.TryGetValue(id, out AbstractPersistentData data))
            {
                throw new InvalidOperationException($"Trying to get {nameof(EntityPersistentData<T>)} with {nameof(id)} of {id} but it doesn't exist in the lookup! Did you call {nameof(CreateEntityPersistentData)}?");
            }

            return (EntityPersistentData<T>)data;
        }

        internal PersistentData<T> GetPersistentData<T>(string id) where T : unmanaged
        {
            if (!m_Lookup.TryGetValue(id, out AbstractPersistentData data))
            {
                throw new InvalidOperationException($"Trying to get {nameof(PersistentData<T>)} with {nameof(id)} of {id} but it doesn't exist in the lookup! Did you call {nameof(CreatePersistentData)}?");
            }

            return (PersistentData<T>)data;
        }
    }
}