using Anvil.Unity.DOTS.Data;
using Unity.Collections;
using Unity.Mathematics;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class ActiveArrayData<TInstance> : AbstractData
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private static readonly int INITIAL_SIZE = (int)math.ceil(ChunkUtil.MaxElementsPerChunk<EntityProxyInstanceWrapper<TInstance>>() / 8.0f);
        
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
            //TODO: Make this part of the constructor
            m_Active = new DeferredNativeArray<EntityProxyInstanceWrapper<TInstance>>(Allocator.Persistent);
            m_Active.SetCapacity(INITIAL_SIZE);
            
            ScheduleInfo = m_Active.ScheduleInfo;
        }

        protected sealed override void DisposeData()
        {
            m_Active.Dispose();
        }
    }
}
