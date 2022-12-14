using Anvil.Unity.DOTS.Data;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class ActiveArrayData<T> : AbstractData
        where T : unmanaged
    {
        private DeferredNativeArray<T> m_Active;

        public DeferredNativeArrayScheduleInfo ScheduleInfo { get; }

        public NativeArray<T> DeferredJobArray
        {
            get => m_Active.AsDeferredJobArray();
        }

        public ActiveArrayData(uint id) : base(id)
        {
            m_Active = new DeferredNativeArray<T>(Allocator.Persistent, Allocator.TempJob);
            ScheduleInfo = m_Active.ScheduleInfo;
        }

        protected sealed override void DisposeData()
        {
            m_Active.Dispose();
        }
    }
}
