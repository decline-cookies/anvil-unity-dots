using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Anvil.Unity.DOTS.Entities
{
    public interface ICompleteData<T>
        where T : struct
    {
        public UnsafeTypedStream<T>.Writer CompletedWriter
        {
            get;
        }
    }

    public struct SystemJobDataWithUpdate<T>
        where T : struct, ICompleteData<T>
    {
        [NativeSetThreadIndex] [ReadOnly] private readonly int m_NativeThreadIndex;

        private readonly NativeArray<T> m_Current;
        private UnsafeTypedStream<T>.Writer m_ContinueWriter;

        private UnsafeTypedStream<T>.LaneWriter m_ContinueLaneWriter;
        private int m_LaneIndex;

        public int Length
        {
            get => m_Current.Length;
        }

        public SystemJobDataWithUpdate(NativeArray<T> current, UnsafeTypedStream<T>.Writer continueWriter)
        {
            m_Current = current;
            m_ContinueWriter = continueWriter;
            m_ContinueLaneWriter = default;

            m_LaneIndex = -1;
            m_NativeThreadIndex = -1;
        }

        public void InitForThread()
        {
            if (!m_ContinueLaneWriter.IsCreated)
            {
                m_LaneIndex = ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex);
                m_ContinueLaneWriter = m_ContinueWriter.AsLaneWriter(m_LaneIndex);
            }
        }

        public T this[int index]
        {
            get => m_Current[index];
        }

        public void Continue(T value)
        {
            m_ContinueLaneWriter.Write(value);
        }

        public void Complete(T value)
        {
            value.CompletedWriter.AsLaneWriter(m_LaneIndex).Write(value);
        }
    }

    public interface ISystemDataCompleteContext<T>
        where T : struct, ICompleteData<T>
    {
        
    }

    public class SystemData<T> : AbstractAnvilBase
        where T : struct, ICompleteData<T>
    {
        private readonly AccessController m_AccessController;
        
        private UnsafeTypedStream<T> m_New;
        private DeferredNativeArray<T> m_Current;
        
        
        //SCHEDULING - IJobDeferredNativeArrayForBatch
        public DeferredNativeArray<T> ArrayForScheduling
        {
            get => m_Current;
        }

        public int BatchSize
        {
            get;
        }
        //END SCHEDULING

        public SystemData()
        {
            m_AccessController = new AccessController();
            m_New = new UnsafeTypedStream<T>(Allocator.Persistent, Allocator.TempJob);

            BatchSize = ChunkUtil.MaxElementsPerChunk<T>();
        }

        protected override void DisposeSelf()
        {
            m_AccessController.Dispose();
            m_New.Dispose();
            m_Current.Dispose();

            base.DisposeSelf();
        }

        public UnsafeTypedStream<T>.Writer GetCompleteWriter()
        {
            return m_New.AsWriter();
        }

        public JobHandle AcquireForComplete()
        {
            return m_AccessController.AcquireAsync(AccessType.ExclusiveWrite);
        }

        public void ReleaseFromComplete(JobHandle releaseAccessDependency)
        {
            m_AccessController.ReleaseAsync(releaseAccessDependency);
        }


        public JobHandle AcquireForNewAsync(out UnsafeTypedStream<T>.Writer newWriter)
        {
            newWriter = m_New.AsWriter();
            return m_AccessController.AcquireAsync(AccessType.SharedWrite);
        }

        public void ReleaseForNewAsync(JobHandle releaseAccessDependency)
        {
            m_AccessController.ReleaseAsync(releaseAccessDependency);
        }

        public JobHandle AcquireForUpdate(JobHandle dependsOn, out SystemJobDataWithUpdate<T> systemJobDataWithUpdate)
        {
            JobHandle updateHandle = m_AccessController.AcquireAsync(AccessType.ExclusiveWrite);

            m_Current = new DeferredNativeArray<T>(Allocator.TempJob);


            ConsolidateToNativeArrayJob<T> consolidateJob = new ConsolidateToNativeArrayJob<T>(m_New.AsReader(),
                                                                                               m_Current);
            JobHandle consolidateHandle = consolidateJob.Schedule(JobHandle.CombineDependencies(dependsOn,
                                                                                                updateHandle));

            JobHandle clearHandle = m_New.Clear(consolidateHandle);
            m_AccessController.ReleaseAsync(clearHandle);

            systemJobDataWithUpdate = new SystemJobDataWithUpdate<T>(m_Current.AsDeferredJobArray(),
                                                                     m_New.AsWriter());

            return clearHandle;
        }

        public void ReleaseFromUpdate(JobHandle releaseAccessDependency)
        {
            m_Current.Dispose(releaseAccessDependency);
        }
    }
}
