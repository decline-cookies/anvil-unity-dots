using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using Anvil.CSharp.Data;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DataSource<T> : AbstractAnvilBase,
                                   IDataSource
        where T : unmanaged, IEquatable<T>
    {
        private readonly IDProvider m_IDProvider;
        private readonly PendingData<T> m_PendingData;
        private readonly Dictionary<uint, AbstractData> m_ActiveDataLookupByID;
        
        public UnsafeTypedStream<T>.Writer PendingWriter { get; }

        public DataSource()
        {
            m_IDProvider = new IDProvider();
            m_PendingData = new PendingData<T>(m_IDProvider.GetNextID());
            PendingWriter = m_PendingData.PendingWriter;
            m_ActiveDataLookupByID = new Dictionary<uint, AbstractData>();
        }

        protected sealed override void DisposeSelf()
        {
            m_IDProvider.Dispose();
            m_PendingData.Dispose();
            m_ActiveDataLookupByID.DisposeAllValuesAndClear();
            base.DisposeSelf();
        }

        public ActiveArrayData<T> CreateActiveArrayData()
        {
            ActiveArrayData<T> activeArrayData = new ActiveArrayData<T>(m_IDProvider.GetNextID());
            m_ActiveDataLookupByID.Add(activeArrayData.ID, activeArrayData);
            return activeArrayData;
        }

        public LiveLookupData<T> CreateActiveLookupData()
        {
            LiveLookupData<T> liveLookupData = new LiveLookupData<T>(m_IDProvider.GetNextID());
            m_ActiveDataLookupByID.Add(liveLookupData.ID, liveLookupData);
            return liveLookupData;
        }

        public JobHandle AcquirePendingAsync(AccessType accessType)
        {
            return m_PendingData.AcquireAsync(accessType);
        }

        public void ReleasePendingAsync(JobHandle dependsOn)
        {
            m_PendingData.ReleaseAsync(dependsOn);
        }
    }
}
