using Anvil.Unity.DOTS.Data;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class PendingData<T> : AbstractData
        where T : unmanaged
    {
        private UnsafeTypedStream<T> m_Pending;
        private readonly unsafe void* m_PendingWriterPointer;
        
        public UnsafeTypedStream<T>.Writer PendingWriter { get; }

        public unsafe PendingData(uint id) : base(id)
        {
            //TODO: Sizing?
            m_Pending = new UnsafeTypedStream<T>(Allocator.Persistent);
            PendingWriter = m_Pending.AsWriter();
            m_PendingWriterPointer = PendingWriter.GetBufferPointer();
        }

        protected sealed override void DisposeData()
        {
            m_Pending.Dispose();
        }
    }
}
