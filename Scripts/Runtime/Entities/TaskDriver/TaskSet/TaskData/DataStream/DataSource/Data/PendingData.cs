using Anvil.Unity.DOTS.Data;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class PendingData<TInstance> : AbstractData
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> m_Pending;
        
        public UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> Pending { get => m_Pending; }
        public UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.Writer PendingWriter { get; }
        public unsafe void* PendingWriterPointer { get; }

        public unsafe PendingData(uint id) : base(id)
        {
            //TODO: Sizing?
            m_Pending = new UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>(Allocator.Persistent);
            PendingWriter = m_Pending.AsWriter();
            PendingWriterPointer = PendingWriter.GetBufferPointer();
        }

        protected sealed override void DisposeData()
        {
            m_Pending.Dispose();
        }
    }
}
