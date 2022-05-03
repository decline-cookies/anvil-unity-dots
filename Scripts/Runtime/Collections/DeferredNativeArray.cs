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
    
    /// <summary>
    /// A native collection similar to <see cref="NativeArray{T}"/> but intended for use in a deferred context.
    /// </summary>
    /// <remarks>
    /// This could be accomplished using a <see cref="NativeList{T}"/> but this class is more clear about its intent
    /// and guards against potential misuse.
    /// </remarks>
    /// <typeparam name="T">The type to contain in the <see cref="DeferredNativeArray{T}"/></typeparam>
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

            throw new InvalidOperationException($"{(object)typeof(T)} used in {nameof(DeferredNativeArray<T>)}<{(object)typeof(T)}> must be unmanaged (contain no managed types) and cannot itself be a native container type.");
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
            
            array.m_Allocator = allocator;
            
            DisposeSentinel.Create(out array.m_Safety, out array.m_DisposeSentinel, 1, allocator);
            InitStaticSafetyId(ref array.m_Safety);
        }

        // ReSharper disable once StaticMemberInGenericType
        private static int s_StaticSafetyId;

        [NativeDisableUnsafePtrRestriction] internal unsafe DeferredNativeArrayBufferInfo* m_BufferInfo;

        [NativeSetClassTypeToNullOnSchedule] private DisposeSentinel m_DisposeSentinel;

        internal AtomicSafetyHandle m_Safety;
        private Allocator m_Allocator;

        /// <summary>
        /// Whether the collection has been created or not.
        /// </summary>
        public unsafe bool IsCreated
        {
            get => m_BufferInfo != null && m_BufferInfo->Buffer != null;
        }

        /// <summary>
        /// Creates a new instance of <see cref="DeferredNativeArray{T}"/>
        /// </summary>
        /// <param name="allocator">The <see cref="Allocator"/> to use for memory allocation.</param>
        public DeferredNativeArray(Allocator allocator)
        {
            Allocate(allocator, out this);
        }
        
        /// <summary>
        /// Disposes the collection
        /// </summary>
        [WriteAccessRequired]
        public unsafe void Dispose()
        {
            if (!UnsafeUtility.IsValidAllocator(m_Allocator))
            {
                throw new InvalidOperationException("The NativeArray can not be Disposed because it was not allocated with a valid allocator.");
            }

            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);

            if (m_BufferInfo == null)
            {
                return;
            }

            UnsafeUtility.Free(m_BufferInfo->Buffer, m_Allocator);
            m_BufferInfo->Buffer = null;
            UnsafeUtility.Free(m_BufferInfo, m_Allocator);
            m_BufferInfo = null;
        }
        
        /// <summary>
        /// Disposes the collection by scheduling a job to free the memory.
        /// NOTE: The collection is considered disposed immediately, only the memory backing the data is freed later on
        /// in case other jobs are using it still.
        /// </summary>
        /// <param name="inputDeps">The <see cref="JobHandle"/> to wait upon before freeing the backing
        /// memory.</param>
        /// <returns>A <see cref="JobHandle"/> for when the disposal is complete.</returns>
        public unsafe JobHandle Dispose(JobHandle inputDeps)
        {
            DisposeSentinel.Clear(ref m_DisposeSentinel);
            DisposeJob disposeJob = new DisposeJob(m_BufferInfo, m_Allocator);
            JobHandle jobHandle = disposeJob.Schedule(inputDeps);
            AtomicSafetyHandle.Release(m_Safety);
            m_BufferInfo->Buffer = null;
            m_BufferInfo = null;
            return jobHandle;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [BurstDiscard]
        private unsafe void AssertForDeferredCreate()
        {
            Debug.Assert(m_BufferInfo->State == DeferredNativeArrayState.Placeholder, $"{nameof(DeferredNativeArray<T>)} has already been created! Cannot call {nameof(DeferredCreate)} more than once.");
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

        /// <summary>
        /// Creates the actual array for when you know the length.
        /// Usually inside a job.
        /// </summary>
        /// <param name="newLength">The new length for the array to be.</param>
        /// <param name="nativeArrayOptions">The <see cref="NativeArrayOptions"/> for initializing the array memory.</param>
        public unsafe NativeArray<T> DeferredCreate(int newLength, NativeArrayOptions nativeArrayOptions = NativeArrayOptions.ClearMemory)
        {
            AssertForDeferredCreate();

            //Allocate the new memory
            long size = SIZE * newLength;
            void* newMemory = UnsafeUtility.Malloc(size, ALIGNMENT, m_Allocator);
            if (nativeArrayOptions == NativeArrayOptions.ClearMemory)
            {
                UnsafeUtility.MemClear(newMemory, size);
            }

            //Free the original memory
            UnsafeUtility.Free(m_BufferInfo->Buffer, m_Allocator);
            //Update the buffer info
            m_BufferInfo->Length = newLength;
            m_BufferInfo->MaxIndex = newLength - 1;
            m_BufferInfo->Buffer = newMemory;
            m_BufferInfo->State = DeferredNativeArrayState.Created;

            return m_InternalNativeArray;
        }
        
        /// <summary>
        /// Returns a <see cref="NativeArray{T}"/> for use in a job.
        /// Initially this <see cref="NativeArray{T}"/> will not have anything in it but later on after
        /// <see cref="DeferredCreate"/> is called, the <see cref="NativeArray{T}"/> instance returned will point to
        /// the same memory. By scheduling your jobs, you can have a job populate the <see cref="DeferredNativeArray{T}"/>
        /// and a subsequent job read from this <see cref="NativeArray{T}"/> before you know the full length.
        /// </summary>
        /// <returns>A <see cref="NativeArray{T}"/> instance that will be populated in the future.</returns>
        public unsafe NativeArray<T> AsDeferredJobArray()
        {
            if (m_InternalNativeArray.IsCreated
             && m_InternalNativeArray.Length > 0)
            {
                return m_InternalNativeArray;
            }
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
#endif
            byte* buffer = (byte*)m_BufferInfo;
            // Unity uses this as an indicator to the internal Job Scheduling code that it needs to defer scheduling until
            // the array length is actually known. 
            buffer += 1;
            m_InternalNativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(buffer, 0, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref m_InternalNativeArray, m_Safety);
#endif

            return m_InternalNativeArray;
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
