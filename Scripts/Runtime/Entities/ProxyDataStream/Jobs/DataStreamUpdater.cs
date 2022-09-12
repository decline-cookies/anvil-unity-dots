using Anvil.CSharp.Data;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Collections;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    //TODO: Docs
    [BurstCompatible]
    public struct DataStreamUpdater<TInstance>
        where TInstance : unmanaged, IProxyInstance
    {
        private const int UNSET_LANE_INDEX = -1;

        [ReadOnly] private readonly UnsafeTypedStream<ProxyInstanceWrapper<TInstance>>.Writer m_ContinueWriter;
        [ReadOnly] private readonly NativeArray<ProxyInstanceWrapper<TInstance>> m_Iteration;

        private UnsafeTypedStream<ProxyInstanceWrapper<TInstance>>.LaneWriter m_ContinueLaneWriter;

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

        public byte CurrentContext
        {
            get;
            private set;
        }

        internal DataStreamUpdater(UnsafeTypedStream<ProxyInstanceWrapper<TInstance>>.Writer continueWriter,
                                   NativeArray<ProxyInstanceWrapper<TInstance>> iteration)
        {
            m_ContinueWriter = continueWriter;
            m_Iteration = iteration;

            m_ContinueLaneWriter = default;
            LaneIndex = UNSET_LANE_INDEX;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_State = UpdaterState.Uninitialized;
#endif

            //TODO: This is odd to have this here. https://github.com/decline-cookies/anvil-unity-dots/pull/54#discussion_r961026947
            CurrentContext = ByteIDProvider.UNSET_ID;
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
                // ReSharper disable once ConvertIfStatementToSwitchStatement
                if (m_State == UpdaterState.Uninitialized)
                {
                    throw new InvalidOperationException($"{nameof(InitForThread)} must be called first before attempting to get an element.");
                }

                if (m_State == UpdaterState.Modifying)
                {
                    throw new InvalidOperationException($"Trying to get an element but the previous element wasn't handled. Please ensure that {nameof(ProxyDataStreamExtension.ContinueOn)} or {nameof(ProxyDataStreamExtension.Resolve)} gets called before the next iteration.");
                }

                m_State = UpdaterState.Modifying;
#endif
                ProxyInstanceWrapper<TInstance> instanceWrapper = m_Iteration[index];
                CurrentContext = instanceWrapper.InstanceID.Context;
                return instanceWrapper.Payload;
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
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (m_State == UpdaterState.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(InitForThread)} must be called first before attempting to continue an element.");
            }

            if (m_State == UpdaterState.Ready)
            {
                throw new InvalidOperationException($"Attempting to call {nameof(Continue)} on a {instance} but that element didn't come from this {nameof(DataStreamUpdater<TInstance>)}. Please ensure that the indexer was called first.");
            }

            m_State = UpdaterState.Ready;
#endif
            m_ContinueLaneWriter.Write(new ProxyInstanceWrapper<TInstance>(instance.Entity,
                                                                           CurrentContext,
                                                                           ref instance));
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
                throw new InvalidOperationException($"Attempting to call {nameof(Resolve)} for an element that didn't come from this {nameof(DataStreamUpdater<TInstance>)}. Please ensure that the indexer was called first.");
            }

            Debug.Assert(m_State == UpdaterState.Modifying);
            m_State = UpdaterState.Ready;
#endif
        }
    }
}
