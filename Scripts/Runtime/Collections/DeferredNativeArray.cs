using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Collections
{
    [BurstCompatible]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DeferredNativeArrayBufferInfo
    {
        public static readonly int SIZE = UnsafeUtility.SizeOf<DeferredNativeArrayBufferInfo>();
        public static readonly int ALIGNMENT = UnsafeUtility.AlignOf<DeferredNativeArrayBufferInfo>();

        [NativeDisableUnsafePtrRestriction] public void* Buffer;
        public int Length;
        public int MaxIndex;
        public DeferredNativeArrayState State;
    }

    internal enum DeferredNativeArrayState
    {
        Placeholder,
        Created
    }
    
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Length = {Length}")]
    [NativeContainer]
    [BurstCompatible]
    public struct DeferredNativeArray<T> : INativeDisposable
        where T : struct
    {
        private static readonly int SIZE = UnsafeUtility.SizeOf<T>();
        private static readonly int ALIGNMENT = UnsafeUtility.AlignOf<T>();

        [BurstDiscard]
        private static void InitStaticSafetyId(ref AtomicSafetyHandle handle)
        {
            if (s_StaticSafetyId == 0)
            {
                s_StaticSafetyId = AtomicSafetyHandle.NewStaticSafetyId<NativeArray<T>>();
            }

            AtomicSafetyHandle.SetStaticSafetyId(ref handle, s_StaticSafetyId);
        }

        [BurstDiscard]
        private static void IsUnmanagedAndThrow()
        {
            if (UnsafeUtility.IsValidNativeContainerElementType<T>())
            {
                return;
            }

            throw new InvalidOperationException($"{(object)typeof(T)} used in NativeArray<{(object)typeof(T)}> must be unmanaged (contain no managed types) and cannot itself be a native container type.");
        }

        private static unsafe void Allocate(Allocator allocator, out DeferredNativeArray<T> array)
        {
            if (allocator <= Allocator.None)
            {
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));
            }

            IsUnmanagedAndThrow();
            array = new DeferredNativeArray<T>();
            void* initialBuffer = UnsafeUtility.Malloc(SIZE, ALIGNMENT, allocator);
            array.m_BufferInfo = (DeferredNativeArrayBufferInfo*)UnsafeUtility.Malloc(DeferredNativeArrayBufferInfo.SIZE,
                                                                                      DeferredNativeArrayBufferInfo.ALIGNMENT,
                                                                                      allocator);
            array.m_BufferInfo->Length = 0;
            array.m_BufferInfo->MaxIndex = 0;
            array.m_BufferInfo->Buffer = initialBuffer;
            array.m_BufferInfo->State = DeferredNativeArrayState.Placeholder;
            array.m_AllocatorLabel = allocator;
            DisposeSentinel.Create(out array.m_Safety, out array.m_DisposeSentinel, 1, allocator);
            InitStaticSafetyId(ref array.m_Safety);
        }

        // ReSharper disable once StaticMemberInGenericType
        private static int s_StaticSafetyId;

        [NativeDisableUnsafePtrRestriction] internal unsafe DeferredNativeArrayBufferInfo* m_BufferInfo;

        [NativeSetClassTypeToNullOnSchedule] private DisposeSentinel m_DisposeSentinel;

        internal AtomicSafetyHandle m_Safety;
        private Allocator m_AllocatorLabel;

        public unsafe int Length
        {
            get =>
                m_BufferInfo != null
                    ? m_BufferInfo->Length
                    : 0;
        }

        public unsafe bool IsCreated
        {
            get => m_BufferInfo != null && m_BufferInfo->Buffer != null;
        }

        public DeferredNativeArray(Allocator allocator)
        {
            Allocate(allocator, out this);
        }

        [WriteAccessRequired]
        public unsafe void Dispose()
        {
            if (!UnsafeUtility.IsValidAllocator(m_AllocatorLabel))
            {
                throw new InvalidOperationException("The NativeArray can not be Disposed because it was not allocated with a valid allocator.");
            }

            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);

            if (m_BufferInfo == null)
            {
                return;
            }

            UnsafeUtility.Free(m_BufferInfo->Buffer, m_AllocatorLabel);
            m_BufferInfo->Buffer = null;
            UnsafeUtility.Free(m_BufferInfo, m_AllocatorLabel);
            m_BufferInfo = null;
        }

        public unsafe JobHandle Dispose(JobHandle inputDeps)
        {
            DisposeSentinel.Clear(ref m_DisposeSentinel);
            DisposeJob disposeJob = new DisposeJob(m_BufferInfo, m_AllocatorLabel);
            JobHandle jobHandle = disposeJob.Schedule(inputDeps);
            AtomicSafetyHandle.Release(m_Safety);
            m_BufferInfo->Buffer = null;
            m_BufferInfo = null;
            return jobHandle;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private unsafe void CheckElementReadAccess(int index)
        {
            if (index < 0
             || index > m_BufferInfo->MaxIndex)
            {
                FailOutOfRangeError(index);
            }

            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private unsafe void CheckElementWriteAccess(int index)
        {
            if (index < 0
             || index > m_BufferInfo->MaxIndex)
            {
                FailOutOfRangeError(index);
            }

            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
        }

        private unsafe void FailOutOfRangeError(int index)
        {
            if (index < m_BufferInfo->Length
             && m_BufferInfo->MaxIndex != m_BufferInfo->Length - 1)
            {
                throw new IndexOutOfRangeException($"Index {(object)index} is out of restricted IJobParallelFor range [{(object)0}...{(object)m_BufferInfo->MaxIndex}] in ReadWriteBuffer.\n" + "ReadWriteBuffers are restricted to only read & write the element at the job index. You can use double buffering strategies to avoid race conditions due to reading & writing in parallel to the same elements from a job.");
            }

            throw new IndexOutOfRangeException($"Index {(object)index} is out of range of '{(object)m_BufferInfo->Length}' Length.");
        }

        public unsafe T this[int index]
        {
            get
            {
                //TODO: Ensure not in placeholder mode
                CheckElementReadAccess(index);
                return UnsafeUtility.ReadArrayElement<T>(m_BufferInfo->Buffer, index);
            }
            [WriteAccessRequired] set
            {
                //TODO: Ensure not in placeholder mode
                CheckElementWriteAccess(index);
                UnsafeUtility.WriteArrayElement(m_BufferInfo->Buffer, index, value);
            }
        }

        public unsafe void DeferredCreate(int newLength, NativeArrayOptions nativeArrayOptions = NativeArrayOptions.ClearMemory)
        {
            //TODO: Ensure not in placeholder mode

            //Allocate the new memory
            long size = SIZE * newLength;
            void* newMemory = UnsafeUtility.Malloc(size, ALIGNMENT, m_AllocatorLabel);
            if (nativeArrayOptions == NativeArrayOptions.ClearMemory)
            {
                UnsafeUtility.MemClear(newMemory, size);
            }

            //Free the original memory
            UnsafeUtility.Free(m_BufferInfo->Buffer, m_AllocatorLabel);
            //Update the buffer info
            m_BufferInfo->Length = newLength;
            m_BufferInfo->MaxIndex = newLength - 1;
            m_BufferInfo->Buffer = newMemory;
            m_BufferInfo->State = DeferredNativeArrayState.Created;
        }

        public unsafe NativeArray<T> AsDeferredJobArray()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
#endif
            byte* buffer = (byte*)m_BufferInfo;
            // We use the first bit of the pointer to infer that the array is in list mode
            // Thus the job scheduling code will need to patch it.
            buffer += 1;
            NativeArray<T> array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(buffer, 0, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_Safety);
#endif

            return array;
        }

        //TODO: Implement copies and other helper functions if needed


        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private readonly unsafe struct DisposeJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] private readonly DeferredNativeArrayBufferInfo* m_BufferInfo;
            private readonly Allocator m_Allocator;

            public DisposeJob(DeferredNativeArrayBufferInfo* bufferInfo, Allocator allocator)
            {
                m_BufferInfo = bufferInfo;
                m_Allocator = allocator;
            }

            public void Execute()
            {
                //This dispose job just handles freeing the memory, the other aspects of the collection were already
                //when this job was scheduled because it requires main thread access
                UnsafeUtility.Free(m_BufferInfo->Buffer, m_Allocator);
                UnsafeUtility.Free(m_BufferInfo, m_Allocator);
            }
        }
    }

    [BurstCompatible]
    public static unsafe class DeferredNativeArrayUnsafeUtility
    {
        public static void* GetBufferInfoUnchecked<T>(ref DeferredNativeArray<T> deferredNativeArray)
            where T : struct
        {
            return deferredNativeArray.m_BufferInfo;
        }

        public static AtomicSafetyHandle GetSafetyHandle<T>(ref DeferredNativeArray<T> deferredNativeArray)
            where T : struct
        {
            //TODO: Collections checks
            return deferredNativeArray.m_Safety;
        }

        public static void* GetSafetyHandlePointer<T>(ref DeferredNativeArray<T> deferredNativeArray)
            where T : struct
        {
            //TODO: Collections checks
            return UnsafeUtility.AddressOf(ref deferredNativeArray.m_Safety);
        }
    }
}
