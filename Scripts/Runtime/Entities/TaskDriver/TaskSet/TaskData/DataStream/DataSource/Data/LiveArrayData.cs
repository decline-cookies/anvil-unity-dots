using Anvil.Unity.DOTS.Data;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class LiveArrayData<T> : AbstractData
        where T : unmanaged
    {
        private DeferredNativeArray<T> m_Live;

        public LiveArrayData(uint id) : base(id)
        {
            m_Live = new DeferredNativeArray<T>(Allocator.Persistent, Allocator.TempJob);
        }

        protected sealed override void DisposeData()
        {
            m_Live.Dispose();
        }
    }
}
