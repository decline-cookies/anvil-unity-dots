using Anvil.Unity.DOTS.Data;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class ActiveArrayData<TInstance> : AbstractData
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> m_Active;

        public DeferredNativeArrayScheduleInfo ScheduleInfo { get; }

        public NativeArray<EntityProxyInstanceWrapper<TInstance>> DeferredJobArray
        {
            get => m_Active.AsDeferredJobArray();
        }

        public DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>> Active
        {
            get => m_Active;
        }

        public ActiveArrayData(uint id) : base(id)
        {
            m_Active = new DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>>(Allocator.Persistent, Allocator.TempJob);
            ScheduleInfo = m_Active.ScheduleInfo;
        }

        protected sealed override void DisposeData()
        {
            m_Active.Dispose();
        }
    }
}
