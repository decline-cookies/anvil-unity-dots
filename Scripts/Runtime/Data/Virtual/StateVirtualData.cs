using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Data
{
    public interface IStateWriterVirtualData<TState>
        where TState : struct
    {
        StateJobAddWriter<TState> AcquireStateJobAddWriter();
        void ReleaseStateJobAddWriter();

        JobHandle AcquireStateJobAddWriterAsync(out StateJobAddWriter<TState> writer);
        void ReleaseStateJobAddWriterAsync(JobHandle releaseAccessDependency);
        
        //TODO: Add Removes
    }

    public interface IStateReaderVirtualData<TKey, TState>
        where TKey : struct, IEquatable<TKey>
        where TState : struct, IState<TKey>
    {
        JobHandle AcquireStateJobReaderAsync(out StateJobReader<TKey, TState> stateJobReader);
        void ReleaseStateJobReaderAsync(JobHandle releaseAccessDependency);
    }


    public class StateVirtualData<TKey, TState> : AbstractAnvilBase,
                                                  IStateWriterVirtualData<TState>,
                                                  IStateReaderVirtualData<TKey, TState>
        where TKey : struct, IEquatable<TKey>
        where TState : struct, IState<TKey>
    {
        private readonly AccessController m_AccessController;
        
        //TODO: Could combine the add/remove to one modify lookup
        private UnsafeTypedStream<TState> m_PendingAdd;
        private UnsafeTypedStream<TKey> m_PendingRemove;
        private DeferredNativeArray<TState> m_ActiveStates;
        private NativeHashMap<TKey, TState> m_ActiveStatesLookup;

        public StateVirtualData(int initialCapacity)
        {
            m_AccessController = new AccessController();
            m_PendingAdd = new UnsafeTypedStream<TState>(Allocator.Persistent, Allocator.TempJob);
            m_PendingRemove = new UnsafeTypedStream<TKey>(Allocator.Persistent, Allocator.TempJob);
            m_ActiveStatesLookup = new NativeHashMap<TKey, TState>(initialCapacity, Allocator.Persistent);
        }

        protected override void DisposeSelf()
        {
            m_AccessController.Dispose();
            m_PendingAdd.Dispose();
            m_PendingRemove.Dispose();
            m_ActiveStatesLookup.Dispose();
            m_ActiveStates.Dispose();
            base.DisposeSelf();
        }

        //*************************************************************************************************************
        // IStateWriter
        //*************************************************************************************************************
        

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

        public JobHandle AcquireStateJobReaderAsync(out StateJobReader<TKey, TState> stateJobReader)
        {
            //TODO: Collections checks
            JobHandle readerHandle = m_AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            stateJobReader = new StateJobReader<TKey, TState>(m_PendingRemove.AsWriter(), 
                                                              m_ActiveStates.AsDeferredJobArray());
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

        public JobHandle RefreshStates(JobHandle dependsOn)
        {
            JobHandle exclusiveWriter = m_AccessController.AcquireAsync(AccessType.ExclusiveWrite);

            //TODO: Investigate reusing a DeferredNativeArray and Clearing instead
            JobHandle disposeOldActiveStatesHandle = m_ActiveStates.Dispose(exclusiveWriter);
            m_ActiveStates = new DeferredNativeArray<TState>(Allocator.TempJob);

            UpdateActiveStatesJob updateActiveStatesJob = new UpdateActiveStatesJob(m_PendingAdd.AsReader(),
                                                                                    m_PendingRemove.AsReader(),
                                                                                    m_ActiveStatesLookup,
                                                                                    m_ActiveStates);
            JobHandle updateHandle = updateActiveStatesJob.Schedule(JobHandle.CombineDependencies(dependsOn, disposeOldActiveStatesHandle));

            JobHandle clearAddHandle = m_PendingAdd.Clear(updateHandle);
            JobHandle clearRemoveHandle = m_PendingRemove.Clear(updateHandle);

            JobHandle clearCompleteHandle = JobHandle.CombineDependencies(clearAddHandle, clearRemoveHandle);

            m_AccessController.ReleaseAsync(clearCompleteHandle);

            return clearCompleteHandle;
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
                NativeArray<TKey> pendingRemove = new NativeArray<TKey>(m_PendingRemoveReader.Count(), Allocator.Temp);
                m_PendingRemoveReader.CopyTo(ref pendingRemove);
                
                NativeArray<TState> pendingAdd = new NativeArray<TState>(m_PendingAddReader.Count(), Allocator.Temp);
                m_PendingAddReader.CopyTo(ref pendingAdd);

                if (pendingRemove.Length <= 0
                 && pendingAdd.Length <= 0)
                {
                    //TODO: If we get the clearing for the native array in, we can probably avoid having to 
                    //do this because we could keep it around and only rebuild if we add or remove
                    UpdateActiveStatesFromLookup();
                    return;
                }

                for (int i = 0; i < pendingRemove.Length; ++i)
                {
                    TKey key = pendingRemove[i];
                    m_ActiveStatesLookup.Remove(key);
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
                NativeArray<TState> statesInLookup = m_ActiveStatesLookup.GetValueArray(Allocator.Temp);
                NativeArray<TState> statesArray = m_ActiveStates.DeferredCreate(statesInLookup.Length);

                void* statesInLookupPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(statesInLookup);
                void* statesArrayPtr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(statesArray);
                long length = statesInLookup.Length * UnsafeUtility.SizeOf<TState>();

                UnsafeUtility.MemCpy(statesArrayPtr, statesInLookupPtr, length);
            }
        }
    }
}
