using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    [BurstCompatible]
    public unsafe struct ThreadPersistentDataAccessor<TData> 
        where TData : unmanaged, IThreadPersistentDataInstance
    {
        private const int UNSET_LANE_INDEX = -1;

        [NativeDisableUnsafePtrRestriction] private readonly void* m_ThreadDataArrayPointer;

        private int m_LaneIndex;

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

        public void InitForThread(int nativeThreadIndex)
        {
            Debug_EnsureInitThreadOnlyCalledOnce();
            m_LaneIndex = ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex);
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
                throw new InvalidOperationException($"{nameof(InitForThread)} has already been called!");
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
                throw new InvalidOperationException($"{nameof(InitForThread)} must be called first before attempting to access thread data.");
            }
#endif
        }
    }
}