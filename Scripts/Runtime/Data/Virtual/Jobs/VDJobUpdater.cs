using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Anvil.Unity.DOTS.Data
{
    [BurstCompatible]
    public struct VDJobUpdater<TKey, TInstance>
        where TKey : struct, IEquatable<TKey>
        where TInstance : struct, ILookupData<TKey>
    {
        private const int DEFAULT_LANE_INDEX = -1;

        [ReadOnly] private readonly UnsafeTypedStream<TInstance>.Writer m_ContinueWriter;
        [ReadOnly] private readonly NativeArray<TInstance> m_Iteration;
        [ReadOnly] private readonly UnsafeHashMap<TKey, TInstance> m_Lookup;

        private UnsafeTypedStream<TInstance>.LaneWriter m_ContinueLaneWriter;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        //TODO: Change to int
        private bool m_IsInitializedForThread;
        private bool m_IsModifying;
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


        public VDJobUpdater(UnsafeTypedStream<TInstance>.Writer continueWriter,
                                  NativeArray<TInstance> iteration,
                                  UnsafeHashMap<TKey, TInstance> lookup)
        {
            m_ContinueWriter = continueWriter;
            m_Iteration = iteration;
            m_Lookup = lookup;

            m_ContinueLaneWriter = default;
            LaneIndex = DEFAULT_LANE_INDEX;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_IsInitializedForThread = false;
            m_IsModifying = false;
#endif
        }

        public void InitForThread(int nativeThreadIndex)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Debug.Assert(!m_IsInitializedForThread);
            m_IsInitializedForThread = true;
#endif

            LaneIndex = ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex);
            m_ContinueLaneWriter = m_ContinueWriter.AsLaneWriter(LaneIndex);
        }

        //TODO: Would it be better to have this be a named method?
        public TInstance this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                Debug.Assert(m_IsInitializedForThread);
                Debug.Assert(!m_IsModifying);
                m_IsModifying = true;
#endif

                return m_Iteration[index];
            }
        }

        //TODO: Maybe put this in another struct?
        public TInstance this[TKey key]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                Debug.Assert(m_IsInitializedForThread);
                Debug.Assert(!m_IsModifying);
                m_IsModifying = true;
#endif

                return m_Lookup[key];
            }
        }

        public void Continue(TInstance value)
        {
            Continue(ref value);
        }

        public void Continue(ref TInstance value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Debug.Assert(m_IsInitializedForThread);
            Debug.Assert(m_IsModifying);
            m_IsModifying = false;
#endif
            m_ContinueLaneWriter.Write(ref value);
        }

        internal void Complete()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Debug.Assert(m_IsInitializedForThread);
            Debug.Assert(m_IsModifying);
            m_IsModifying = false;
#endif
        }
    }
}
