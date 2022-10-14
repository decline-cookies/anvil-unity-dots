using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [BurstCompatible]
    public struct DynamicBufferSharedWriter<T>
        where T : struct, IBufferElementData
    {
        private const int UNSET_DB_INDEX = -1;
        
        [NativeDisableContainerSafetyRestriction] [WriteOnly] private BufferFromEntity<T> m_BFE;

        private int m_DBIndex;

        internal DynamicBufferSharedWriter(SystemBase system) : this()
        {
            m_BFE = system.GetBufferFromEntity<T>(false);
            m_DBIndex = UNSET_DB_INDEX;
            
            Debug_InitializeWriterState();
        }

        public void InitForThread(int nativeThreadIndex)
        {
            Debug_EnsureInitThreadOnlyCalledOnce();

            m_DBIndex = ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex);
        }

        public T this[Entity entity]
        {
            get
            {
                Debug_EnsureCanAccess();
                return m_BFE[entity][m_DBIndex];
            }
            set
            {
                Debug_EnsureCanAccess();
                DynamicBuffer<T> db = m_BFE[entity];
                db[m_DBIndex] = value;
            }
        }
        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************
        
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private enum WriterState
        {
            Uninitialized,
            Ready
        }

        private WriterState m_State;
#endif
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_InitializeWriterState()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_State = WriterState.Uninitialized;
#endif
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureInitThreadOnlyCalledOnce()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_State != WriterState.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(InitForThread)} has already been called!");
            }

            m_State = WriterState.Ready;
#endif
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureCanAccess()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_State == WriterState.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(InitForThread)} must be called first before attempting to access an element.");
            }
#endif
        }
    }
}
