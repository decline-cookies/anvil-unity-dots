using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Collections;
using UnityEngine;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// Represents a read/write reference to <see cref="VirtualData{TKey,TInstance}"/>
    /// for use in updating the data.
    /// </summary>
    /// <remarks>
    /// Commonly used to iterate through all instances, perform some work and either
    /// <see cref="Continue(TInstance)"/> if more work needs to be done next frame or
    /// <see cref="Resolve"/> if the work is done.
    /// </remarks>
    /// <typeparam name="TKey">The type of key to use for lookup of the instance</typeparam>
    /// <typeparam name="TInstance">The type of instance</typeparam>
    [BurstCompatible]
    public struct VDUpdater<TInstance>
        where TInstance : unmanaged, IKeyedData
    {
        private const int UNSET_LANE_INDEX = -1;

        [ReadOnly] private readonly UnsafeTypedStream<TInstance>.Writer m_ContinueWriter;
        [ReadOnly] private readonly UnsafeTypedStream<TInstance>.Writer m_CancelWriter;
        [ReadOnly] private VDLookupReader<bool> m_CancelLookup;
        [ReadOnly] private readonly NativeArray<TInstance> m_Iteration;

        private UnsafeTypedStream<TInstance>.LaneWriter m_ContinueLaneWriter;
        private UnsafeTypedStream<TInstance>.LaneWriter m_CancelLaneWriter;

        private int m_CancelLookupCount;

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

        internal VDUpdater(UnsafeTypedStream<TInstance>.Writer continueWriter,
                           UnsafeTypedStream<TInstance>.Writer cancelWriter,
                           VDLookupReader<bool> cancelLookup,
                           NativeArray<TInstance> iteration)
        {
            m_ContinueWriter = continueWriter;
            m_CancelWriter = cancelWriter;
            m_CancelLookup = cancelLookup;
            m_Iteration = iteration;

            m_ContinueLaneWriter = default;
            m_CancelLaneWriter = default;
            LaneIndex = UNSET_LANE_INDEX;

            m_CancelLookupCount = 0;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_State = UpdaterState.Uninitialized;
#endif
        }

        /// <summary>
        /// Initializes based on the thread it's being used on.
        /// This must be called before doing anything else with the struct.
        /// </summary>
        /// <remarks>
        /// Anvil Jobs (<see cref="IAnvilJob"/>, <see cref="IAnvilJobForDefer"/>, etc)
        /// will call this automatically.
        /// </remarks>
        /// <param name="nativeThreadIndex">The native thread index</param>
        public void InitForThread(int nativeThreadIndex)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_State != UpdaterState.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(InitForThread)} has already been called!");
            }

            m_State = UpdaterState.Ready;
#endif

            LaneIndex = ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex);
            m_ContinueLaneWriter = m_ContinueWriter.AsLaneWriter(LaneIndex);
            m_CancelLaneWriter = m_CancelWriter.AsLaneWriter(LaneIndex);

            m_CancelLookupCount = m_CancelLookup.Count();
        }

        public bool TryGetInstance(int index, out TInstance instance)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (m_State == UpdaterState.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(InitForThread)} must be called first before attempting to get an element.");
            }

            if (m_State == UpdaterState.Modifying)
            {
                throw new InvalidOperationException($"Trying to get an element but the previous element wasn't handled. Please ensure that {nameof(VirtualDataExtensions.ContinueOn)} or {nameof(VirtualDataExtensions.Resolve)} gets called before the next iteration.");
            }

            m_State = UpdaterState.Modifying;
#endif
            instance = m_Iteration[index];

            if (m_CancelLookupCount > 0 && m_CancelLookup.ContainsKey(instance.ContextID))
            {
                Cancel(ref instance);
                return false;
            }

            return true;
        }

        private void Cancel(ref TInstance instance)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (m_State == UpdaterState.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(InitForThread)} must be called first before attempting to continue an element.");
            }

            if (m_State == UpdaterState.Ready)
            {
                throw new InvalidOperationException($"Attempting to call {nameof(Cancel)} on a {instance} but that element didn't come from this {nameof(VDUpdater<TInstance>)}. Please ensure that the indexer was called first.");
            }

            m_State = UpdaterState.Ready;
#endif
            m_CancelLaneWriter.Write(ref instance);
        }

        /// <summary>
        /// Gets a <typeparamref name="TInstance"/> at the specified index.
        /// </summary>
        /// <param name="index">The index to the backing array</param>
//         public TInstance this[int index]
//         {
//             get
//             {
// #if ENABLE_UNITY_COLLECTIONS_CHECKS
//                 // ReSharper disable once ConvertIfStatementToSwitchStatement
//                 if (m_State == UpdaterState.Uninitialized)
//                 {
//                     throw new InvalidOperationException($"{nameof(InitForThread)} must be called first before attempting to get an element.");
//                 }
//
//                 if (m_State == UpdaterState.Modifying)
//                 {
//                     throw new InvalidOperationException($"Trying to get an element but the previous element wasn't handled. Please ensure that {nameof(VirtualDataExtensions.ContinueOn)} or {nameof(VirtualDataExtensions.Resolve)} gets called before the next iteration.");
//                 }
//
//                 m_State = UpdaterState.Modifying;
// #endif
//
//                 return m_Iteration[index];
//             }
//         }

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
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (m_State == UpdaterState.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(InitForThread)} must be called first before attempting to continue an element.");
            }

            if (m_State == UpdaterState.Ready)
            {
                throw new InvalidOperationException($"Attempting to call {nameof(Continue)} on a {instance} but that element didn't come from this {nameof(VDUpdater<TInstance>)}. Please ensure that the indexer was called first.");
            }

            m_State = UpdaterState.Ready;
#endif

            m_ContinueLaneWriter.Write(ref instance);
        }

        internal void Resolve()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (m_State == UpdaterState.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(InitForThread)} must be called first before attempting to resolve an element.");
            }

            if (m_State == UpdaterState.Ready)
            {
                throw new InvalidOperationException($"Attempting to call {nameof(Resolve)} for an element that didn't come from this {nameof(VDUpdater<TInstance>)}. Please ensure that the indexer was called first.");
            }

            Debug.Assert(m_State == UpdaterState.Modifying);
            m_State = UpdaterState.Ready;
#endif
        }
    }
}
