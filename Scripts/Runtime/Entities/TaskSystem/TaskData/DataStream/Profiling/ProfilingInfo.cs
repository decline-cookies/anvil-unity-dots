using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;
using Unity.Profiling.LowLevel;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [BurstCompatible]
    public unsafe struct ProfilingInfo : IDisposable
    {
        [BurstCompatible]
        private struct Buffer
        {
            public static readonly int SIZE = UnsafeUtility.SizeOf<Buffer>();
            public static readonly int ALIGNMENT = UnsafeUtility.AlignOf<Buffer>();
            
            public int LiveInstances;
            public int LiveCapacity;
            public int PendingCapacity;
        }
        
        public readonly ProfilerMarker ProfilerMarker;
        [NativeDisableUnsafePtrRestriction] private Buffer* m_Buffer;

        
        public int LiveInstances
        {
            get => m_Buffer->LiveInstances;
            set => m_Buffer->LiveInstances = value;
        }
        
        public int LiveCapacity
        {
            get => m_Buffer->LiveCapacity;
            set => m_Buffer->LiveCapacity = value;
        }
        
        public int PendingCapacity
        {
            get => m_Buffer->PendingCapacity;
            set => m_Buffer->PendingCapacity = value;
        }

        internal ProfilingInfo(AbstractDataStream dataStream) : this()
        {
            m_Buffer = (Buffer*)UnsafeUtility.Malloc(Buffer.SIZE, Buffer.ALIGNMENT, Allocator.Persistent);
            ProfilerMarker = new ProfilerMarker(ProfilerCategory.Scripts,
                                               dataStream.ToString(),
                                               MarkerFlags.Script);
        }

        public void Dispose()
        {
            if (m_Buffer == null)
            {
                return;
            }
            UnsafeUtility.Free(m_Buffer, Allocator.Persistent);
            m_Buffer = null;
        }
    }
}
