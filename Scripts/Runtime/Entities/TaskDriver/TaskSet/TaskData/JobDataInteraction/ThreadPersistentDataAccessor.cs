using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Represents a read/write reference to an <see cref="IThreadPersistentData{TData}"/>
    /// </summary>
    /// <typeparam name="TData">The type of <see cref="IThreadPersistentDataInstance"/> to read/write</typeparam>
    [GenerateTestsForBurstCompatibility]
    public unsafe struct ThreadPersistentDataAccessor<TData> 
        where TData : unmanaged, IThreadPersistentDataInstance
    {
        private const int UNSET_LANE_INDEX = -1;

        [NativeDisableUnsafePtrRestriction] private readonly void* m_ThreadDataArrayPointer;

        private int m_LaneIndex;

        /// <summary>
        /// A reference to the data for reading/writing
        /// </summary>
        public ref TData ThreadData
        {
            get
            {
                Debug_EnsureCanAccess();
                return ref UnsafeUtility.ArrayElementAsRef<TData>(m_ThreadDataArrayPointer, m_LaneIndex);
            }
        }

        internal ThreadPersistentDataAccessor(ref UnsafeArray<TData> threadDataArray) : this()
        {
            m_ThreadDataArrayPointer = threadDataArray.GetUnsafePtr();
            m_LaneIndex = UNSET_LANE_INDEX;

            Debug_InitializeAccessorState();
        }
        
        /// <summary>
        /// Call once to initialize the state of this accessor for the thread it is running on.
        /// </summary>
        /// <param name="nativeThreadIndex">The native thread index that the job is running on</param>
        public void InitForThread(int nativeThreadIndex)
        {
            Debug_EnsureInitThreadOnlyCalledOnce();
            m_LaneIndex = ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex);
        }
        
        /// <summary>
        /// Call once to initialize the state of this writer for main thread usage.
        /// </summary>
        public void InitForMainThread()
        {
            Debug_EnsureInitThreadOnlyCalledOnce();
            m_LaneIndex = ParallelAccessUtil.CollectionIndexForMainThread();
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private enum AccessorState
        {
            Uninitialized,
            Ready
        }

        private AccessorState m_State;
#endif

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_InitializeAccessorState()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_State = AccessorState.Uninitialized;
#endif
        }

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_EnsureInitThreadOnlyCalledOnce()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_State != AccessorState.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(InitForThread)} or {nameof(InitForMainThread)} has already been called!");
            }

            m_State = AccessorState.Ready;
#endif
        }

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_EnsureCanAccess()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_State == AccessorState.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(InitForThread)} or {nameof(InitForMainThread)} must be called first before attempting to access thread data.");
            }
#endif
        }
    }
}