using Anvil.Unity.DOTS.Data;
using System;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class PendingData<T> : AbstractData
        where T : unmanaged, IEquatable<T>
    {
        private UnsafeTypedStream<T> m_Pending;

        public UnsafeTypedStream<T> Pending { get => m_Pending; }
        public UnsafeTypedStream<T>.Writer PendingWriter { get; }
        public unsafe void* PendingWriterPointer { get; }

        public unsafe PendingData(
            IDataOwner dataOwner, 
            string uniqueContextIdentifier) 
            : base(
                dataOwner, 
                CancelRequestBehaviour.Ignore, 
                null, 
                uniqueContextIdentifier)
        {
            m_Pending = new UnsafeTypedStream<T>(Allocator.Persistent);
            PendingWriter = m_Pending.AsWriter();
            PendingWriterPointer = PendingWriter.GetBufferPointer();
        }

        protected sealed override void DisposeData()
        {
            m_Pending.Dispose();
        }
    }
}
