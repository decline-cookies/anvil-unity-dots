using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// A native collection similar to <see cref="NativeArray{T}"/> but intended for use in a deferred context.
    /// Useful for cases where a job that hasn't finished yet will determine the length of the array.
    ///
    /// Upon creation, this collection is an empty array that cannot be interacted with.
    /// You can pass it into a job as a <see cref="NativeArray{T}"/> using <see cref="AsDeferredJobArray"/>.
    /// That array will be empty and only populated later on when the job is run.
    ///
    /// To populate, pass this <see cref="DeferredNativeArray{T}"/> into a job and then call
    /// <see cref="DeferredCreate"/> when the length of the array is known. This will allocate the right
    /// size of <see cref="NativeArray{T}"/> to act on in your job and populate.
    ///
    /// In your later jobs that were provided references using <see cref="AsDeferredJobArray"/> the collection
    /// will be populated properly.
    /// </summary>
    /// <remarks>
    /// This could be accomplished using a <see cref="NativeList{T}"/> but this class is more clear about its intent
    /// and guards against potential misuse.
    /// </remarks>
    /// <typeparam name="T">The type to contain in the <see cref="DeferredNativeArray{T}"/></typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [BurstCompatible]
    public struct DeferredNativeArray<T> : INativeDisposable
        where T : struct
    {
        //*************************************************************************************************************
        // INTERNAL STRUCTS
        //*************************************************************************************************************

        [BurstCompatible]
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct BufferInfo
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public static readonly int SIZE = UnsafeUtility.SizeOf<BufferInfo>();

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public static readonly int ALIGNMENT = UnsafeUtility.AlignOf<BufferInfo>();

            [NativeDisableUnsafePtrRestriction] public void* Buffer;
            public int Length;
            public Allocator DeferredAllocator;
        }

        //*************************************************************************************************************
        // STATIC HELPERS
        //*************************************************************************************************************

        private static readonly int SIZE = UnsafeUtility.SizeOf<T>();
        private static readonly int ALIGNMENT = UnsafeUtility.AlignOf<T>();

        // ReSharper disable once StaticMemberInGenericType
        private static int s_StaticSafetyId;

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
        private static void AssertValidElementType()
        {
            if (UnsafeUtility.IsValidNativeContainerElementType<T>())
            {
                return;
            }

            throw new InvalidOperationException($"{(object)typeof(T)} used in {nameof(DeferredNativeArray<T>)}<{(object)typeof(T)}> must be unmanaged (contain no managed types) and cannot itself be a native container type.");
        }

        private static unsafe void Allocate(Allocator allocator, Allocator deferredAllocator, out DeferredNativeArray<T> array)
        {
            //Ensures that deferred allocator can only be Temp, TempJob or Persistent and that the allocator is at the same or more persistent level.
            //Can't have a deferred allocator that is persistent and a temp allocator.
            Assert.IsTrue(deferredAllocator <= allocator && deferredAllocator > Allocator.None);

            AssertValidElementType();
            array = new DeferredNativeArray<T>();
            array.m_BufferInfo = (BufferInfo*)UnsafeUtility.Malloc(BufferInfo.SIZE,
                                                                   BufferInfo.ALIGNMENT,
                                                                   allocator);
            array.m_BufferInfo->Length = 0;
            array.m_BufferInfo->Buffer = null;
            array.m_BufferInfo->DeferredAllocator = deferredAllocator;

            array.m_Allocator = allocator;

            DisposeSentinel.Create(out array.m_Safety, out array.m_DisposeSentinel, 1, allocator);
            InitStaticSafetyId(ref array.m_Safety);
        }

        private static unsafe void ClearBufferInfo(BufferInfo* bufferInfo)
        {
            if (bufferInfo == null || bufferInfo->Buffer == null)
            {
                return;
            }

            UnsafeUtility.Free(bufferInfo->Buffer, bufferInfo->DeferredAllocator);
            bufferInfo->Buffer = null;
            bufferInfo->Length = 0;
        }

        private static unsafe void DisposeBufferInfo(BufferInfo* bufferInfo, Allocator allocator)
        {
            if (bufferInfo == null)
            {
                return;
            }
            ClearBufferInfo(bufferInfo);
            UnsafeUtility.Free(bufferInfo, allocator);
        }

        //*************************************************************************************************************
        // NATIVE COLLECTION
        //*************************************************************************************************************

        [NativeDisableUnsafePtrRestriction] internal unsafe BufferInfo* m_BufferInfo;

        [NativeSetClassTypeToNullOnSchedule] private DisposeSentinel m_DisposeSentinel;

        internal AtomicSafetyHandle m_Safety;
        private Allocator m_Allocator;

        /// <summary>
        /// Whether the collection has been created or not.
        /// </summary>
        public unsafe bool IsCreated
        {
            get => m_BufferInfo != null;
        }

        public unsafe int Length
        {
            get =>
                m_BufferInfo != null
                    ? m_BufferInfo->Length
                    : 0;
        }

        /// <summary>
        /// Creates a new instance of <see cref="DeferredNativeArray{T}"/>
        /// </summary>
        /// <param name="allocator">
        /// The <see cref="Allocator"/> to use for memory allocation of the collection and
        /// the deferred data.
        /// </param>
        public DeferredNativeArray(Allocator allocator) : this(allocator, allocator)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="DeferredNativeArray{T}"/>
        /// </summary>
        /// <param name="allocator">
        /// The <see cref="Allocator"/> to use for memory allocation of the collection only.
        /// </param>
        /// <param name="deferredAllocator">
        /// The <see cref="Allocator"/> to use for memory allocation of the deferred data.
        /// </param>
        public DeferredNativeArray(Allocator allocator, Allocator deferredAllocator)
        {
            Allocate(allocator, deferredAllocator, out this);
        }

        /// <summary>
        /// Disposes the collection
        /// </summary>
        [WriteAccessRequired]
        public unsafe void Dispose()
        {
            if (!IsCreated)
            {
                return;
            }

            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
            DisposeBufferInfo(m_BufferInfo, m_Allocator);
            m_BufferInfo = null;
        }

        /// <summary>
        /// Clears all data in the collection
        /// </summary>
        [WriteAccessRequired]
        public unsafe void Clear()
        {
            ClearBufferInfo(m_BufferInfo);
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
            if (!IsCreated)
            {
                return default;
            }

            DisposeSentinel.Clear(ref m_DisposeSentinel);
            DisposeJob disposeJob = new DisposeJob(m_BufferInfo, m_Allocator);
            JobHandle jobHandle = disposeJob.Schedule(inputDeps);
            AtomicSafetyHandle.Release(m_Safety);
            m_BufferInfo = null;
            return jobHandle;
        }

        /// <summary>
        /// Schedules the clearing of the collections
        /// </summary>
        /// <param name="inputDeps">The <see cref="JobHandle"/> to wait on before clearing.</param>
        /// <returns>A <see cref="JobHandle"/> for when clearing is complete</returns>
        public unsafe JobHandle Clear(JobHandle inputDeps)
        {
            if (!IsCreated)
            {
                return default;
            }

            ClearJob clearJob = new ClearJob(m_BufferInfo);
            JobHandle jobHandle = clearJob.Schedule(inputDeps);
            return jobHandle;
        }

        /// <summary>
        /// Creates the actual array for when you know the length.
        /// Usually inside a job.
        /// </summary>
        /// <param name="newLength">The new length for the array to be.</param>
        /// <param name="nativeArrayOptions">The <see cref="NativeArrayOptions"/> for initializing the array memory.</param>
        public unsafe NativeArray<T> DeferredCreate(int newLength, NativeArrayOptions nativeArrayOptions = NativeArrayOptions.ClearMemory)
        {
            //Allocate the new memory
            long size = SIZE * newLength;
            void* newMemory = UnsafeUtility.Malloc(size, ALIGNMENT, m_BufferInfo->DeferredAllocator);
            if (nativeArrayOptions == NativeArrayOptions.ClearMemory)
            {
                UnsafeUtility.MemClear(newMemory, size);
            }

            //Update the buffer info
            m_BufferInfo->Length = newLength;
            m_BufferInfo->Buffer = newMemory;

            //Return an actual NativeArray so it's familiar to use and we don't have to reimplement the same api and functionality
            NativeArray<T> array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(m_BufferInfo->Buffer, newLength, m_BufferInfo->DeferredAllocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_Safety);
#endif
            return array;
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
            //This whole function taken from NativeList.AsDeferredJobArray
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
#endif
            byte* buffer = (byte*)m_BufferInfo;
            // Unity uses this as an indicator to the internal Job Scheduling code that it needs to defer scheduling until
            // the array length is actually known. 
            buffer += 1;
            NativeArray<T> array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(buffer, 0, Allocator.Invalid);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_Safety);
#endif

            return array;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private readonly unsafe struct DisposeJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] private readonly BufferInfo* m_BufferInfo;
            private readonly Allocator m_Allocator;

            public DisposeJob(BufferInfo* bufferInfo, Allocator allocator)
            {
                m_BufferInfo = bufferInfo;
                m_Allocator = allocator;
            }

            public void Execute()
            {
                DisposeBufferInfo(m_BufferInfo, m_Allocator);
            }
        }

        [BurstCompile]
        private readonly unsafe struct ClearJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] private readonly BufferInfo* m_BufferInfo;

            public ClearJob(BufferInfo* bufferInfo)
            {
                m_BufferInfo = bufferInfo;
            }

            public void Execute()
            {
                ClearBufferInfo(m_BufferInfo);
            }
        }
    }
}
