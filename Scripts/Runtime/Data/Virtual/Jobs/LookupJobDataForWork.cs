using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Anvil.Unity.DOTS.Data
{
    public struct LookupJobDataForWork<TKey, TValue>
        where TKey : struct, IEquatable<TKey>
        where TValue : struct, ILookupValue<TKey>
    {
        private const int DEFAULT_LANE_INDEX = -1;
        
        [ReadOnly] private readonly UnsafeTypedStream<TKey>.Writer m_RemoveWriter;
        [ReadOnly] private readonly UnsafeHashMap<TKey, TValue> m_Lookup;
        [ReadOnly] private readonly NativeArray<TValue> m_Iteration;

        private UnsafeTypedStream<TKey>.LaneWriter m_RemoveLaneWriter;
        
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        //TODO: Change to int
        private bool m_IsInitializedForThread;
#endif

        public int LaneIndex
        {
            get;
            private set;
        }

        public int Length
        {
            get => m_Iteration.Length;
        }
        
        
        public LookupJobDataForWork(UnsafeTypedStream<TKey>.Writer removeWriter, 
                                    UnsafeHashMap<TKey, TValue> lookup, 
                                    NativeArray<TValue> iteration)
        {
            m_RemoveWriter = removeWriter;
            m_Lookup = lookup;
            m_Iteration = iteration;

            m_RemoveLaneWriter = default;
            LaneIndex = DEFAULT_LANE_INDEX;
            
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_IsInitializedForThread = false;
#endif
        }
        
        public void InitForThread(int nativeThreadIndex)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Debug.Assert(!m_IsInitializedForThread);
            m_IsInitializedForThread = true;
#endif

            LaneIndex = ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex);
            m_RemoveLaneWriter = m_RemoveWriter.AsLaneWriter(LaneIndex);
        }
        
        public TValue this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                Debug.Assert(m_IsInitializedForThread);
#endif

                return m_Iteration[index];
            }
        }
        
        public TValue this[TKey key]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                Debug.Assert(m_IsInitializedForThread);
#endif

                return m_Lookup[key];
            }
        }

        public void Remove(TKey key)
        {
            Remove(ref key);
        }

        public void Remove(ref TKey key)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Debug.Assert(m_IsInitializedForThread);
#endif
            m_RemoveLaneWriter.Write(ref key);
        }
    }
}
