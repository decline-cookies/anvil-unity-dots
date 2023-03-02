using Anvil.CSharp.Collections;
using Anvil.CSharp.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract partial class AbstractPersistentDataSystem : AbstractAnvilSystemBase
    {
        private static readonly Dictionary<uint, AbstractPersistentData> s_ThreadLookup = new Dictionary<uint, AbstractPersistentData>();
        private static int s_InstanceCount;
        private static readonly IDProvider s_IDProvider = new IDProvider();
        
        private readonly Dictionary<uint, AbstractPersistentData> m_Lookup;
        
        protected AbstractPersistentDataSystem()
        {
            s_InstanceCount++;
            m_Lookup = new Dictionary<uint, AbstractPersistentData>();
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
                s_IDProvider.Dispose();
            }
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            //Just in case
            Enabled = false;
        }


        protected uint CreatePersistentData<T>(T persistentData) where T : unmanaged
        {
            uint id = s_IDProvider.GetNextID();
            m_Lookup.Add(id, new PersistentData<T>(id, persistentData));
            return id;
        }

        protected uint CreateThreadPersistentData<T>(
            IThreadPersistentData<T>.ConstructionCallbackPerThread constructionCallbackPerThread,
            IThreadPersistentData<T>.DisposalCallbackPerThread disposalCallbackPerThread = null)
            where T : unmanaged
        {
            uint id = s_IDProvider.GetNextID();
            s_ThreadLookup.Add(id, new ThreadPersistentData<T>(id, constructionCallbackPerThread, disposalCallbackPerThread));
            return id;
        }

        protected uint CreateEntityPersistentData<T>(
            IEntityPersistentData<T>.DisposalCallbackPerEntity disposalCallbackPerEntity = null)
            where T : unmanaged
        {
            uint id = s_IDProvider.GetNextID();
            m_Lookup.Add(id, new EntityPersistentData<T>(id, disposalCallbackPerEntity));
            return id;
        }

        public IEntityPersistentData<T> AcquireEntityPersistentData<T>(uint id, AccessType accessType)
            where T : unmanaged
        {
            EntityPersistentData<T> entityPersistentData = GetEntityPersistentData<T>(id);
            entityPersistentData.Acquire(accessType);

            return entityPersistentData;
        }

        public IThreadPersistentData<T> AcquireThreadPersistentData<T>(uint id, AccessType accessType)
            where T : unmanaged
        {
            ThreadPersistentData<T> threadPersistentData = GetThreadPersistentData<T>(id);
            threadPersistentData.Acquire(accessType);

            return threadPersistentData;
        }

        public IPersistentData<T> AcquirePersistentData<T>(uint id, AccessType accessType) where T : unmanaged
        {
            PersistentData<T> persistentData = GetPersistentData<T>(id);
            persistentData.Acquire(accessType);

            return persistentData;
        }


        internal ThreadPersistentData<T> GetThreadPersistentData<T>(uint id) where T : unmanaged
        {
            if (!s_ThreadLookup.TryGetValue(id, out AbstractPersistentData data))
            {
                throw new InvalidOperationException($"Trying to get {nameof(ThreadPersistentData<T>)} with {nameof(id)} of {id} but it doesn't exist in the lookup! Did you call {nameof(CreateThreadPersistentData)}?");
            }

            return (ThreadPersistentData<T>)data;
        }

        internal EntityPersistentData<T> GetEntityPersistentData<T>(uint id) where T : unmanaged
        {
            if (!m_Lookup.TryGetValue(id, out AbstractPersistentData data))
            {
                throw new InvalidOperationException($"Trying to get {nameof(EntityPersistentData<T>)} with {nameof(id)} of {id} but it doesn't exist in the lookup! Did you call {nameof(CreateEntityPersistentData)}?");
            }

            return (EntityPersistentData<T>)data;
        }

        internal PersistentData<T> GetPersistentData<T>(uint id) where T : unmanaged
        {
            if (!m_Lookup.TryGetValue(id, out AbstractPersistentData data))
            {
                throw new InvalidOperationException($"Trying to get {nameof(PersistentData<T>)} with {nameof(id)} of {id} but it doesn't exist in the lookup! Did you call {nameof(CreatePersistentData)}?");
            }

            return (PersistentData<T>)data;
        }
    }
}