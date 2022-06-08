using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Entities;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Data
{


    public interface IStateVirtualData
    {
        void UnregisterResponseChannel(IStateVirtualData virtualData);
        JobHandle AcquireForResponse();
        void ReleaseForResponse(JobHandle releaseAccessDependency);
    }

    //TODO: Serialization and Deserialization
    public class StateVirtualData<TKey, TState> : AbstractAnvilBase,
                                                  IStateVirtualData
        where TKey : struct, IEquatable<TKey>
        where TState : struct, ILookupValue<TKey>
    {
        private static readonly int STATE_SIZE = UnsafeUtility.SizeOf<TState>();
        
        private readonly AccessController m_AccessController;
        
        //We could combine the add and remove into one but then we lose out on being pre-sorted
        //and the NativeHashMap could grow only to then fall back to an already allocated size
        private UnsafeTypedStream<TState> m_PendingAdd;
        private UnsafeTypedStream<TKey> m_PendingRemove;
        private DeferredNativeArray<TState> m_ActiveStates;
        private NativeHashMap<TKey, TState> m_ActiveStatesLookup;
        
        private readonly HashSet<IStateVirtualData> m_ResponseChannels;

        private IStateVirtualData m_Parent;

        public StateVirtualData(int initialCapacity)
        {
            m_AccessController = new AccessController();
            m_PendingAdd = new UnsafeTypedStream<TState>(Allocator.Persistent, Allocator.TempJob);
            m_PendingRemove = new UnsafeTypedStream<TKey>(Allocator.Persistent, Allocator.TempJob);
            m_ActiveStatesLookup = new NativeHashMap<TKey, TState>(initialCapacity, Allocator.Persistent);
            m_ActiveStates = new DeferredNativeArray<TState>(Allocator.Persistent, Allocator.TempJob);
            
            m_ResponseChannels = new HashSet<IStateVirtualData>();
            
            //TODO: Allow for better batching rules - Spread evenly across X threads, maximizing chunk
            BatchSize = ChunkUtil.MaxElementsPerChunk<TState>();
        }

        protected override void DisposeSelf()
        {
            m_ResponseChannels.Clear();
            m_Parent?.UnregisterResponseChannel(this);

            m_AccessController.Dispose();
            m_PendingAdd.Dispose();
            m_PendingRemove.Dispose();
            m_ActiveStatesLookup.Dispose();
            m_ActiveStates.Dispose();
            base.DisposeSelf();
        }
        
        public DeferredNativeArray<TState> ArrayForScheduling
        {
            get => m_ActiveStates;
        }

        //TODO: Balanced batch across X threads
        public int BatchSize
        {
            get;
        }

        public StateVirtualData<RKey, RState> CreateResponseChannel<RKey, RState>(int initialCapacity)
            where RKey : struct, IEquatable<RKey>
            where RState : struct, ILookupValue<RKey>
        {
            StateVirtualData<RKey, RState> virtualData = new StateVirtualData<RKey, RState>(initialCapacity);
            m_ResponseChannels.Add(virtualData);
            virtualData.AddParent(this);
            return virtualData;
        }

        public void UnregisterResponseChannel(IStateVirtualData virtualData)
        {
            m_ResponseChannels.Remove(virtualData);
        }

        private void AddParent(IStateVirtualData parentVirtualData)
        {
            m_Parent = parentVirtualData;
        }

        //*************************************************************************************************************
        // IStateWriter
        //*************************************************************************************************************

        public StateJobAddWriter<TState> GetStateJobAddWriter()
        {
            return new StateJobAddWriter<TState>(m_PendingAdd.AsWriter());
        }

        public StateJobAddWriter<TState> AcquireStateJobAddWriter()
        {
            //TODO: Collections checks
            m_AccessController.Acquire(AccessType.ExclusiveWrite);
            return new StateJobAddWriter<TState>(m_PendingAdd.AsWriter(), true);
        }

        public void ReleaseStateJobAddWriter()
        {
            //TODO: Collections checks
            m_AccessController.Release();
        }

        public JobHandle AcquireStateJobAddWriterAsync(out StateJobAddWriter<TState> writer)
        {
            //TODO: Collections checks
            JobHandle handle = m_AccessController.AcquireAsync(AccessType.SharedWrite);
            writer = new StateJobAddWriter<TState>(m_PendingAdd.AsWriter());
            return handle;
        }

        public void ReleaseStateJobAddWriterAsync(JobHandle releaseAccessDependency)
        {
            //TODO: Collections checks
            m_AccessController.ReleaseAsync(releaseAccessDependency);
        }
        
        //TODO: Add removals

        //*************************************************************************************************************
        // IStateReader
        //*************************************************************************************************************

        public JobHandle AcquireStateJobReaderAsync(out StateJobUpdater<TKey, TState> stateJobReader)
        {
            //TODO: Collections checks
            JobHandle readerHandle = m_AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            // stateJobReader = new StateJobUpdater<TKey, TState>(m_PendingRemove.AsWriter(), 
            //                                                       m_ActiveStates.AsDeferredJobArray());
            stateJobReader = default;
            return readerHandle;
        }

        public void ReleaseStateJobReaderAsync(JobHandle releaseAccessDependency)
        {
            //TODO: Collections checks
            m_AccessController.ReleaseAsync(releaseAccessDependency);
        }

        public JobHandle AcquireStateJobLookupReaderAsync(out StateJobLookupReader<TKey, TState> stateJobLookupReader)
        {
            //TODO: Collections checks
            JobHandle readerHandle = m_AccessController.AcquireAsync(AccessType.SharedRead);
            stateJobLookupReader = new StateJobLookupReader<TKey, TState>(m_ActiveStatesLookup);
            return readerHandle;
        }

        public void ReleaseStateJobLookupReaderAsync(JobHandle releaseAccessDependency)
        {
            //TODO: Collections checks
            m_AccessController.ReleaseAsync(releaseAccessDependency);
        }

        //*************************************************************************************************************
        // IStateOwner
        //*************************************************************************************************************
        public JobHandle AcquireForResponse()
        {
            return m_AccessController.AcquireAsync(AccessType.SharedWrite);
        }
        
        public void ReleaseForResponse(JobHandle releaseAccessDependency)
        {
           m_AccessController.ReleaseAsync(releaseAccessDependency);
        }
        
        //TODO: Bad name
        public JobHandle AcquireRefreshStates(JobHandle dependsOn, out StateJobUpdater<TKey, TState> stateJobReader)
        {
            JobHandle exclusiveWriter = m_AccessController.AcquireAsync(AccessType.ExclusiveWrite);

            UpdateActiveStatesJob updateActiveStatesJob = new UpdateActiveStatesJob(m_PendingAdd.AsReader(),
                                                                                    m_PendingRemove.AsReader(),
                                                                                    m_ActiveStatesLookup,
                                                                                    m_ActiveStates);
            JobHandle updateHandle = updateActiveStatesJob.Schedule(JobHandle.CombineDependencies(dependsOn, exclusiveWriter));

            JobHandle clearAddHandle = m_PendingAdd.Clear(updateHandle);
            JobHandle clearRemoveHandle = m_PendingRemove.Clear(updateHandle);

            JobHandle clearCompleteHandle = JobHandle.CombineDependencies(clearAddHandle, clearRemoveHandle);

            // stateJobReader = new StateJobUpdater<TKey, TState>(m_PendingRemove.AsWriter(), m_ActiveStates.AsDeferredJobArray());
            stateJobReader = default;

            if (m_ResponseChannels.Count == 0)
            {
                return clearCompleteHandle;
            }

            //Get write access to all possible channels that we can write a response to.
            //+1 to include our incoming dependency
            NativeArray<JobHandle> allDependencies = new NativeArray<JobHandle>(m_ResponseChannels.Count + 1, Allocator.Temp);
            allDependencies[0] = clearCompleteHandle;
            int index = 1;
            foreach (IStateVirtualData responseChannel in m_ResponseChannels)
            {
                allDependencies[index] = responseChannel.AcquireForResponse();
                index++;
            }

            return JobHandle.CombineDependencies(allDependencies);
        }

        public void ReleaseRefreshStates(JobHandle releaseAccessDependency)
        {
            m_AccessController.ReleaseAsync(releaseAccessDependency);
            if (m_ResponseChannels.Count == 0)
            {
                return;
            }

            foreach (IStateVirtualData responseChannel in m_ResponseChannels)
            {
                responseChannel.ReleaseForResponse(releaseAccessDependency);
            }
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct UpdateActiveStatesJob : IJob
        {
            [ReadOnly] private readonly UnsafeTypedStream<TState>.Reader m_PendingAddReader;
            [ReadOnly] private readonly UnsafeTypedStream<TKey>.Reader m_PendingRemoveReader;
            private NativeHashMap<TKey, TState> m_ActiveStatesLookup;
            private DeferredNativeArray<TState> m_ActiveStates;

            public UpdateActiveStatesJob(UnsafeTypedStream<TState>.Reader pendingAddReader,
                                         UnsafeTypedStream<TKey>.Reader pendingRemoveReader,
                                         NativeHashMap<TKey, TState> activeStatesLookup,
                                         DeferredNativeArray<TState> activeStates)
            {
                m_PendingAddReader = pendingAddReader;
                m_PendingRemoveReader = pendingRemoveReader;
                m_ActiveStatesLookup = activeStatesLookup;
                m_ActiveStates = activeStates;
            }

            public void Execute()
            {
                NativeArray<TKey> pendingRemove = m_PendingRemoveReader.ToNativeArray(Allocator.Temp);
                NativeArray<TState> pendingAdd = m_PendingAddReader.ToNativeArray(Allocator.Temp);
                
                //If we have nothing to add or remove, we can early return
                if (pendingRemove.Length <= 0
                 && pendingAdd.Length <= 0)
                {
                    return;
                }

                for (int i = 0; i < pendingRemove.Length; ++i)
                {
                    m_ActiveStatesLookup.Remove(pendingRemove[i]);
                }

                for (int i = 0; i < pendingAdd.Length; ++i)
                {
                    TState state = pendingAdd[i];
                    m_ActiveStatesLookup.Add(state.Key, state);
                }

                UpdateActiveStatesFromLookup();
            }

            private unsafe void UpdateActiveStatesFromLookup()
            {
                m_ActiveStates.Clear();
                
                NativeArray<TState> statesInLookup = m_ActiveStatesLookup.GetValueArray(Allocator.Temp);
                NativeArray<TState> statesArray = m_ActiveStates.DeferredCreate(statesInLookup.Length);

                void* statesInLookupPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(statesInLookup);
                void* statesArrayPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(statesArray);
                long length = statesInLookup.Length * STATE_SIZE;

                UnsafeUtility.MemCpy(statesArrayPtr, statesInLookupPtr, length);
            }
        }
    }
}
