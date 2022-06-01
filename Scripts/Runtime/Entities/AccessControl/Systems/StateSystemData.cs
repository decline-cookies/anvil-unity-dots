using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IStateWriter<TState>
        where TState : struct
    {
        StateJobWriter<TState> AcquireStateJobWriter();
        void ReleaseStateJobWriter();
        
        JobHandle AcquireStateJobWriterAsync(out RequestJobWriter<TState> writer);
        void ReleaseStateJobWriterAsync(JobHandle releaseAccessDependency);
    }

    public interface IStateReader<TState>
        where TState : struct
    {
        JobHandle AcquireStateJobReaderAsync(out StateJobReader<TState> stateJobReader);
        void ReleaseStateJobReaderAsync(JobHandle releaseAccessDependency);
    }

    public interface IStateOwner<TState>
        where TState : struct
    {
        JobHandle RefreshStates(JobHandle dependsOn);
    }

    public class StateSystemData<TState> : AbstractSystemData<TState>,
                                           IStateWriter<TState>,
                                           IStateReader<TState>,
                                           IStateOwner<TState>
        where TState : struct
    {
        //*************************************************************************************************************
        // IStateWriter
        //*************************************************************************************************************

        public StateJobWriter<TState> AcquireStateJobWriter()
        {
            //TODO: Collections checks
            UnsafeTypedStream<TState>.Writer pendingWriter = AcquirePending(AccessType.ExclusiveWrite);
            return new StateJobWriter<TState>(pendingWriter, true);
        }

        public void ReleaseStateJobWriter()
        {
            //TODO: Collections checks
            ReleasePending();
        }

        public JobHandle AcquireStateJobWriterAsync(out RequestJobWriter<TState> writer)
        {
            //TODO: Collections checks
            JobHandle handle = AcquirePendingAsync(AccessType.SharedWrite, out UnsafeTypedStream<TState>.Writer pendingWriter);
            writer = new RequestJobWriter<TState>(pendingWriter);
            return handle;
        }

        public void ReleaseStateJobWriterAsync(JobHandle releaseAccessDependency)
        {
            //TODO: Collections checks
            ReleasePendingAsync(releaseAccessDependency);
        }

        //*************************************************************************************************************
        // IStateReader
        //*************************************************************************************************************

        public JobHandle AcquireStateJobReaderAsync(out StateJobReader<TState> stateJobReader)
        {
            //TODO: Collections checks
            JobHandle readerHandle = AcquireAllAsync(AccessType.ExclusiveWrite, 
                                                     out UnsafeTypedStream<TState>.Writer pendingWriter, 
                                                     out NativeArray<TState> current);
            stateJobReader = new StateJobReader<TState>(pendingWriter,
                                                  current);
            return readerHandle;
        }

        public void ReleaseStateJobReaderAsync(JobHandle releaseAccessDependency)
        {
            //TODO: Collections checks
            ReleaseAllAsync(releaseAccessDependency);
        }

        //*************************************************************************************************************
        // IStateOwner
        //*************************************************************************************************************

        public JobHandle RefreshStates(JobHandle dependsOn)
        {
            return InternalAcquireProcessorAsync(dependsOn);
        }
    }
}
