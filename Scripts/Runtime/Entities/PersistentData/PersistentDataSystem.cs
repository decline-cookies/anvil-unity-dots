using Anvil.CSharp.Collections;
using System;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities
{
    internal partial class PersistentDataSystem : AbstractAnvilSystemBase
    {
        private static readonly Dictionary<Type, AbstractPersistentData> s_ThreadPersistentData = new Dictionary<Type, AbstractPersistentData>();
        private static int s_InstanceCount;

        private readonly Dictionary<Type, AbstractPersistentData> m_EntityPersistentData;
        
        public PersistentDataSystem()
        {
            s_InstanceCount++;
            m_EntityPersistentData = new Dictionary<Type, AbstractPersistentData>();
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
        }

        protected override void OnDestroy()
        {
            m_EntityPersistentData.DisposeAllValuesAndClear();
            s_InstanceCount--;
            if (s_InstanceCount <= 0)
            {
                s_ThreadPersistentData.DisposeAllValuesAndClear();
            }
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            //Just in case
            Enabled = false;
        }

        public ThreadPersistentData<T> GetOrCreateThreadPersistentData<T>()
            where T : unmanaged, IThreadPersistentDataInstance
        {
            Type type = typeof(T);
            if (!s_ThreadPersistentData.TryGetValue(type, out AbstractPersistentData persistentData))
            {
                persistentData = new ThreadPersistentData<T>();
                s_ThreadPersistentData.Add(type, persistentData);
            }
            return (ThreadPersistentData<T>)persistentData;
        }
        
        public EntityPersistentData<T> GetOrCreateEntityPersistentData<T>()
            where T : unmanaged, IEntityPersistentDataInstance
        {
            Type type = typeof(T);
            if (!m_EntityPersistentData.TryGetValue(type, out AbstractPersistentData persistentData))
            {
                persistentData = new EntityPersistentData<T>();
                m_EntityPersistentData.Add(type, persistentData);
            }

            return (EntityPersistentData<T>)persistentData;
        }
    }
}