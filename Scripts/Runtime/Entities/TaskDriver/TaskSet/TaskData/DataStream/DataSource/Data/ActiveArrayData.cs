using Anvil.Unity.DOTS.Data;
using System;
using Unity.Collections;
using Unity.Mathematics;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class ActiveArrayData<T> : AbstractData
        where T : unmanaged, IEquatable<T>
    {
        private static readonly int INITIAL_SIZE = (int)math.ceil(ChunkUtil.MaxElementsPerChunk<T>() / 8.0f);
        
        private DeferredNativeArray<T> m_Active;

        public DeferredNativeArrayScheduleInfo ScheduleInfo { get; }

        public NativeArray<T> DeferredJobArray
        {
            get => m_Active.AsDeferredJobArray();
        }

        public DeferredNativeArray<T> Active
        {
            get => m_Active;
        }

        public ActiveArrayData(uint id) : base(id)
        {
            m_Active = new DeferredNativeArray<T>(Allocator.Persistent);
            //TODO: Make this part of the constructor
            m_Active.SetCapacity(INITIAL_SIZE);
            
            ScheduleInfo = m_Active.ScheduleInfo;
        }

        protected sealed override void DisposeData()
        {
            m_Active.Dispose();
        }
    }
}
