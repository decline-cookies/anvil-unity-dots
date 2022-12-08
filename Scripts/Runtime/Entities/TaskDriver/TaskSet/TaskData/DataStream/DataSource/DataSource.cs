using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using Anvil.CSharp.Data;
using System;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DataSource<T> : AbstractAnvilBase,
                                   IDataSource
        where T : unmanaged, IEquatable<T>
    {
        private readonly IDProvider m_IDProvider;
        private readonly PendingData<T> m_PendingData;
        private readonly Dictionary<uint, AbstractData> m_LiveDataLookupByID;

        public DataSource()
        {
            m_IDProvider = new IDProvider();
            m_PendingData = new PendingData<T>(m_IDProvider.GetNextID());
            m_LiveDataLookupByID = new Dictionary<uint, AbstractData>();
        }

        protected sealed override void DisposeSelf()
        {
            m_IDProvider.Dispose();
            m_PendingData.Dispose();
            m_LiveDataLookupByID.DisposeAllValuesAndClear();
            base.DisposeSelf();
        }

        public LiveArrayData<T> CreateLiveArrayData()
        {
            LiveArrayData<T> liveArrayData = new LiveArrayData<T>(m_IDProvider.GetNextID());
            m_LiveDataLookupByID.Add(liveArrayData.ID, liveArrayData);
            return liveArrayData;
        }

        public LiveLookupData<T> CreateLiveLookupData()
        {
            LiveLookupData<T> liveLookupData = new LiveLookupData<T>(m_IDProvider.GetNextID());
            m_LiveDataLookupByID.Add(liveLookupData.ID, liveLookupData);
            return liveLookupData;
        }
    }
}
