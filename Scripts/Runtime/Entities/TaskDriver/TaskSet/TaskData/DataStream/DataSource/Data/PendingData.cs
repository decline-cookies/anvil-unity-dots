using Anvil.Unity.DOTS.Data;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class PendingData<T> : AbstractData
        where T : unmanaged
    {
        private UnsafeTypedStream<T> m_Pending;
        private readonly UnsafeTypedStream<T>.Writer m_PendingWriter;
        private readonly unsafe void* m_PendingWriterPointer;

        public unsafe PendingData(uint id) : base(id)
        {
            //TODO: Sizing?
            m_Pending = new UnsafeTypedStream<T>(Allocator.Persistent);
            m_PendingWriter = m_Pending.AsWriter();
            m_PendingWriterPointer = m_PendingWriter.GetBufferPointer();
        }

        protected sealed override void DisposeData()
        {
            m_Pending.Dispose();
        }
    }
}
