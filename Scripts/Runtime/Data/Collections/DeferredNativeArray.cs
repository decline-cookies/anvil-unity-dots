using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Debug = UnityEngine.Debug;

namespace Anvil.Unity.DOTS.Data
{
    //TODO: #99 - This class should be renamed and updated to reflect the List like functionality it now has.

    /// <summary>
    /// Scheduling information for a <see cref="DeferredNativeArray{T}"/>
    /// </summary>
    /// <remarks>
    /// Provides access to internal data necessary for job scheduling but avoids
    /// boxing if done with interfaces.
    /// </remarks>
    public unsafe struct DeferredNativeArrayScheduleInfo
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal void* SafetyHandlePtr
        {
            get;
        }
#endif

        internal void* BufferPtr
        {
            get;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal DeferredNativeArrayScheduleInfo(void* safetyHandlePtr, void* bufferPtr)
        {
            SafetyHandlePtr = safetyHandlePtr;
            BufferPtr = bufferPtr;
        }
#else
        internal DeferredNativeArrayScheduleInfo(void* bufferPtr)
        {
            BufferPtr = bufferPtr;
        }
#endif
    }

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
    /// This could be accomplished using a <see cref="NativeList{T}"/> but this is more clear about its intent
    /// and guards against potential misuse.
    /// </remarks>
    /// <typeparam name="T">The type to contain in the <see cref="DeferredNativeArray{T}"/></typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [BurstCompatible]
    public struct DeferredNativeArray<T> : INativeDisposable
        where T : unmanaged
    {
        //*************************************************************************************************************
        // INTERNAL STRUCTS
        //*************************************************************************************************************

        [BurstCompatible]
        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct BufferInfo
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public static readonly int SIZE = UnsafeUtility.SizeOf<BufferInfo>();

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public static readonly int ALIGNMENT = UnsafeUtility.AlignOf<BufferInfo>();

            [NativeDisableUnsafePtrRestriction] public void* Buffer;
            public int Length;
            public int Capacity;
            public Allocator DeferredAllocator;
        }

        //*************************************************************************************************************
        // STATIC HELPERS
        //*************************************************************************************************************

        private static readonly int SIZE = UnsafeUtility.SizeOf<T>();
        private static readonly int ALIGNMENT = UnsafeUtility.AlignOf<T>();

        internal static unsafe DeferredNativeArray<T> ReinterpretFromPointer(void* ptr)
        {
            Debug_EnsurePointerNotNull(ptr);
            DeferredNativeArray<T> array = new DeferredNativeArray<T>();
            array.m_BufferInfo = (BufferInfo*)ptr;
            return array;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
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
#endif

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
            array.m_BufferInfo = (BufferInfo*)UnsafeUtility.Malloc(
                BufferInfo.SIZE,
                BufferInfo.ALIGNMENT,
                allocator);
            array.m_BufferInfo->Length = 0;
            array.m_BufferInfo->Capacity = 0;
            array.m_BufferInfo->Buffer = null;
            array.m_BufferInfo->DeferredAllocator = deferredAllocator;

            array.m_Allocator = allocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out array.m_Safety, out array.m_DisposeSentinel, 1, allocator);
            InitStaticSafetyId(ref array.m_Safety);
#endif
        }

        private static unsafe void ClearBufferInfo(BufferInfo* bufferInfo)
        {
            if (bufferInfo == null)
            {
                return;
            }

            bufferInfo->Length = 0;
        }

        private static unsafe void DisposeBufferInfo(BufferInfo* bufferInfo, Allocator allocator)
        {
            if (bufferInfo == null)
            {
                return;
            }

            if (bufferInfo->Buffer != null)
            {
                UnsafeUtility.Free(bufferInfo->Buffer, bufferInfo->DeferredAllocator);
                bufferInfo->Buffer = null;
                bufferInfo->Length = 0;
                bufferInfo->Capacity = 0;
            }

            UnsafeUtility.Free(bufferInfo, allocator);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static unsafe void Debug_EnsurePointerNotNull(void* ptr)
        {
            if (ptr == null)
            {
                throw new InvalidOperationException($"Trying to reinterpret the {typeof(DeferredNativeArray<T>)} from a pointer but the pointer is null!");
            }
        }

        //*************************************************************************************************************
        // NATIVE COLLECTION
        //*************************************************************************************************************

        [NativeDisableUnsafePtrRestriction] private unsafe BufferInfo* m_BufferInfo;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [NativeSetClassTypeToNullOnSchedule] private DisposeSentinel m_DisposeSentinel;
#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle m_Safety;
#endif
        private Allocator m_Allocator;

        /// <summary>
        /// Whether the collection has been created or not.
        /// </summary>
        public unsafe bool IsCreated
        {
            get => m_BufferInfo != null;
        }

        /// <summary>
        /// The length of the array
        /// </summary>
        public unsafe int Length
        {
            get =>
                m_BufferInfo != null
                    ? m_BufferInfo->Length
                    : 0;
        }

        /// <summary>
        /// The capacity of the array
        /// </summary>
        public unsafe int Capacity
        {
            get =>
                m_BufferInfo != null
                    ? m_BufferInfo->Capacity
                    : 0;
        }

        /// <summary>
        /// Scheduling information about this <see cref="DeferredNativeArray{T}"/> for use in job
        /// scheduling.
        /// </summary>
        public unsafe DeferredNativeArrayScheduleInfo ScheduleInfo
        {
            get =>
                new DeferredNativeArrayScheduleInfo(
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    UnsafeUtility.AddressOf(ref m_Safety),
#endif
                    m_BufferInfo);
        }

        /// <summary>
        /// Creates a new instance of <see cref="DeferredNativeArray{T}"/>
        /// </summary>
        /// <param name="allocator">
        /// The <see cref="Allocator"/> to use for memory allocation of the collection and
        /// the deferred data.
        /// </param>
        public DeferredNativeArray(Allocator allocator) : this(allocator, allocator) { }

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
        /// Disposes the collection and frees all memory
        /// </summary>
        [WriteAccessRequired]
        public unsafe void Dispose()
        {
            if (!IsCreated)
            {
                return;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
            DisposeBufferInfo(m_BufferInfo, m_Allocator);
            m_BufferInfo = null;
        }

        /// <summary>
        /// Clears all data in the collection, does not dispose of the underlying memory though.
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

            DisposeJob disposeJob = new DisposeJob(m_BufferInfo, m_Allocator);
            JobHandle jobHandle = disposeJob.Schedule(inputDeps);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Clear(ref m_DisposeSentinel);
            AtomicSafetyHandle.Release(m_Safety);
#endif
            m_BufferInfo = null;
            return jobHandle;
        }

        /// <summary>
        /// Schedules the clearing of the collections, does not release the backing memory though
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
        /// Sets the desired capacity of the array. This will allocate new memory of the correct size and free any
        /// old memory that was being used. If any elements were in the array, they will be copied into the new memory.
        /// </summary>
        /// <param name="capacity">The desired size of the array</param>
        public unsafe void SetCapacity(int capacity)
        {
            Debug.Assert(m_BufferInfo != null);

            long size = SIZE * capacity;
            void* newMemory = UnsafeUtility.Malloc(size, ALIGNMENT, m_BufferInfo->DeferredAllocator);

            //If we have anything in the buffer already, we need to copy it over
            if (m_BufferInfo->Length > 0)
            {
                UnsafeUtility.MemCpy(newMemory, m_BufferInfo->Buffer, m_BufferInfo->Length * SIZE);
            }

            UnsafeUtility.Free(m_BufferInfo->Buffer, m_BufferInfo->DeferredAllocator);

            m_BufferInfo->Buffer = newMemory;
            m_BufferInfo->Capacity = capacity;
        }

        /// <summary>
        /// Adds an element to next free spot in the array.
        /// Will trigger a re-allocation if going above the current capacity.
        /// </summary>
        /// <param name="element">The element to add to the array</param>
        public unsafe void Add(T element)
        {
            //If we're going to go over, we need to reallocate
            if (m_BufferInfo->Length + 1 > m_BufferInfo->Capacity)
            {
                SetCapacity(m_BufferInfo->Capacity * 2);
            }

            UnsafeUtility.WriteArrayElement(m_BufferInfo->Buffer, m_BufferInfo->Length, element);
            m_BufferInfo->Length++;
        }

        /// <summary>
        /// Creates the actual array for when you know the length.
        /// Usually inside a job.
        /// </summary>
        /// <param name="newLength">The new length for the array to be.</param>
        /// <param name="nativeArrayOptions">The <see cref="NativeArrayOptions"/> for initializing the array memory.</param>
        public unsafe NativeArray<T> DeferredCreate(int newLength, NativeArrayOptions nativeArrayOptions = NativeArrayOptions.ClearMemory)
        {
            //We must exist and be in a pending state. This will prevent multiple calls to DeferredCreate
            //which could leak memory. We can call this again if we've scheduled or performed a Clear.
            //If this triggers, we have been disposed or never created
            Debug.Assert(m_BufferInfo != null);
            //If this triggers, we've already called DeferredCreate.
            //Check scheduling to ensure that a Clear job happened in between the jobs that do a Deferred operation.
            Debug.Assert(m_BufferInfo->Buffer == null);

            //Allocate the new memory
            long size = SIZE * newLength;
            void* newMemory = UnsafeUtility.Malloc(size, ALIGNMENT, m_BufferInfo->DeferredAllocator);
            if (nativeArrayOptions == NativeArrayOptions.ClearMemory)
            {
                UnsafeUtility.MemClear(newMemory, size);
            }

            //Update the buffer info
            m_BufferInfo->Length = newLength;
            m_BufferInfo->Capacity = newLength;
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

        public unsafe NativeArray<T> AsArray()
        {
            //This whole function taken from NativeList.AsDeferredJobArray
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
#endif
            NativeArray<T> array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(m_BufferInfo->Buffer, m_BufferInfo->Length, m_BufferInfo->DeferredAllocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_Safety);
#endif

            return array;
        }

        internal unsafe void* GetBufferPointer()
        {
            return m_BufferInfo;
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