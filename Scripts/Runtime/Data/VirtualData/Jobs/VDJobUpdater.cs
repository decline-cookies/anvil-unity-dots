using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Collections;
using UnityEngine;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// A struct to be used in jobs that is for updating the <see cref="VirtualData{TKey,TInstance}"/>
    /// that this <see cref="VDJobUpdater{TKey,TInstance}"/> represents.
    ///
    /// Commonly used to iterate through all instances, perform some work and either
    /// <see cref="Continue(TInstance)"/> if more work needs to be done next frame or
    /// <see cref="Complete"/> if the work is done.
    /// </summary>
    /// <typeparam name="TKey">The type of key to use for lookup of the instance</typeparam>
    /// <typeparam name="TInstance">The type of instance</typeparam>
    [BurstCompatible]
    public struct VDJobUpdater<TKey, TInstance>
        where TKey : struct, IEquatable<TKey>
        where TInstance : struct, ILookupData<TKey>
    {
        private const int DEFAULT_LANE_INDEX = -1;

        [ReadOnly] private readonly UnsafeTypedStream<TInstance>.Writer m_ContinueWriter;
        [ReadOnly] private readonly NativeArray<TInstance> m_Iteration;

        private UnsafeTypedStream<TInstance>.LaneWriter m_ContinueLaneWriter;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private enum UpdaterState
        {
            Uninitialized,
            Ready,
            Modifying
        }

        private UpdaterState m_State;
#endif

        public int LaneIndex
        {
            get;
            private set;
        }

        internal VDJobUpdater(UnsafeTypedStream<TInstance>.Writer continueWriter,
                              NativeArray<TInstance> iteration)
        {
            m_ContinueWriter = continueWriter;
            m_Iteration = iteration;

            m_ContinueLaneWriter = default;
            LaneIndex = DEFAULT_LANE_INDEX;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_State = UpdaterState.Uninitialized;
#endif
        }
        
        /// <summary>
        /// Initializes the struct based on the thread it's being used on.
        /// This must be called before doing anything else with the struct.
        /// </summary>
        /// <param name="nativeThreadIndex">The native thread index</param>
        public void InitForThread(int nativeThreadIndex)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Debug.Assert(m_State == UpdaterState.Uninitialized);
            m_State = UpdaterState.Ready;
#endif

            LaneIndex = ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex);
            m_ContinueLaneWriter = m_ContinueWriter.AsLaneWriter(LaneIndex);
        }
        
        /// <summary>
        /// Gets a <typeparamref name="TInstance"/> at the specified index.
        /// </summary>
        /// <param name="index">The index to the backing array</param>
        public TInstance this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                Debug.Assert(m_State == UpdaterState.Ready);
                m_State = UpdaterState.Modifying;
#endif

                return m_Iteration[index];
            }
        }
        
        /// <summary>
        /// Signals that this instance should be updated again next frame.
        /// </summary>
        /// <param name="instance">The instance to continue</param>
        public void Continue(TInstance instance)
        {
            Continue(ref instance);
        }
        
        /// <inheritdoc cref="Continue(TInstance)"/>
        public void Continue(ref TInstance instance)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Debug.Assert(m_State == UpdaterState.Modifying);
            m_State = UpdaterState.Ready;
#endif
            m_ContinueLaneWriter.Write(ref instance);
        }

        internal void Complete()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Debug.Assert(m_State == UpdaterState.Modifying);
            m_State = UpdaterState.Ready;
#endif
        }
    }
}
