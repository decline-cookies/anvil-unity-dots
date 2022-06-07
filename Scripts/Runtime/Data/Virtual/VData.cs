using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Data
{
    public interface IVData
    {
        void UnregisterAsCompletionDestination(IVData vData);
        JobHandle AcquireForDestination();
        void ReleaseForDestination(JobHandle releaseAccessDependency);
    }

    public class VData<T> : AbstractAnvilBase,
                            IVData
        where T : struct
    {
        private readonly AccessController m_AccessController;

        private UnsafeTypedStream<T> m_Pending;
        private DeferredNativeArray<T> m_Current;

        private readonly IVData m_Source;
        private readonly HashSet<IVData> m_Destinations;

        public VData() : this(null)
        {
        }

        private VData(IVData source)
        {
            m_AccessController = new AccessController();
            m_Pending = new UnsafeTypedStream<T>(Allocator.Persistent,
                                                 Allocator.TempJob);
            m_Current = new DeferredNativeArray<T>(Allocator.Persistent,
                                                   Allocator.TempJob);
            m_Destinations = new HashSet<IVData>();

            m_Source = source;
            RegisterAsCompletionDestination(m_Source);
        }

        protected override void DisposeSelf()
        {
            m_Destinations.Clear();
            m_Source?.UnregisterAsCompletionDestination(this);
            m_AccessController.Dispose();
            m_Pending.Dispose();
            m_Current.Dispose();

            base.DisposeSelf();
        }

        public VData<TType> CreateDestination<TType>()
            where TType : struct
        {
            VData<TType> data = new VData<TType>(this);
            return data;
        }

        private void RegisterAsCompletionDestination(IVData vData)
        {
            if (vData == null)
            {
                return;
            }
            m_Destinations.Add(vData);
        }

        public void UnregisterAsCompletionDestination(IVData vData)
        {
            m_Destinations.Remove(vData);
        }

        public DeferredNativeArray<T> ArrayForScheduling
        {
            get => m_Current;
        }

        public JobDataForCompletion<T> GetCompletionWriter()
        {
            return new JobDataForCompletion<T>(m_Pending.AsWriter());
        }

        public JobHandle AcquireForDestination()
        {
            //TODO: Collections Checks
            return m_AccessController.AcquireAsync(AccessType.SharedWrite);
        }

        public void ReleaseForDestination(JobHandle releaseAccessDependency)
        {
            //TODO: Collections Checks
            m_AccessController.ReleaseAsync(releaseAccessDependency);
        }

        public JobHandle AcquireForAdd(out JobDataForAdd<T> workStruct)
        {
            //TODO: Collections Checks
            JobHandle sharedWriteHandle = m_AccessController.AcquireAsync(AccessType.SharedWrite);

            workStruct = new JobDataForAdd<T>(m_Pending.AsWriter());

            return sharedWriteHandle;
        }

        public void ReleaseForAdd(JobHandle releaseAccessDependency)
        {
            //TODO: Collections Checks
            m_AccessController.ReleaseAsync(releaseAccessDependency);
        }

        public JobHandle AcquireForWork(JobHandle dependsOn, out JobDataForWork<T> workStruct)
        {
            //TODO: Collections Checks
            JobHandle exclusiveWriteHandle = m_AccessController.AcquireAsync(AccessType.ExclusiveWrite);

            //Consolidate everything in pending into current so it can be balanced across threads
            ConsolidateToNativeArrayJob<T> consolidateJob = new ConsolidateToNativeArrayJob<T>(m_Pending.AsReader(),
                                                                                               m_Current);
            JobHandle consolidateHandle = consolidateJob.Schedule(JobHandle.CombineDependencies(dependsOn, exclusiveWriteHandle));

            //Clear pending so we can use it again
            JobHandle clearHandle = m_Pending.Clear(consolidateHandle);

            //Create the work struct
            workStruct = new JobDataForWork<T>(m_Pending.AsWriter(),
                                               m_Current.AsDeferredJobArray());

            if (m_Destinations.Count == 0)
            {
                return clearHandle;
            }

            //Get write access to all possible channels that we can write a response to.
            //+1 to include our incoming dependency
            NativeArray<JobHandle> allDependencies = new NativeArray<JobHandle>(m_Destinations.Count + 1, Allocator.Temp);
            allDependencies[0] = clearHandle;
            int index = 1;
            foreach (IVData destinationData in m_Destinations)
            {
                allDependencies[index] = destinationData.AcquireForDestination();
                index++;
            }

            return JobHandle.CombineDependencies(allDependencies);
        }

        public void ReleaseForWork(JobHandle releaseAccessDependency)
        {
            //TODO: Collections Checks
            m_AccessController.ReleaseAsync(releaseAccessDependency);
            if (m_Destinations.Count == 0)
            {
                return;
            }

            foreach (IVData destinationData in m_Destinations)
            {
                destinationData.ReleaseForDestination(releaseAccessDependency);
            }
        }
    }

    public struct JobDataForAdd<T>
        where T : struct
    {
        private const int DEFAULT_LANE_INDEX = -1;

        [ReadOnly] private readonly UnsafeTypedStream<T>.Writer m_AddWriter;

        private UnsafeTypedStream<T>.LaneWriter m_AddLaneWriter;

        public int LaneIndex
        {
            get;
            private set;
        }

        public JobDataForAdd(UnsafeTypedStream<T>.Writer addWriter) : this()
        {
            m_AddWriter = addWriter;

            m_AddLaneWriter = default;
            LaneIndex = DEFAULT_LANE_INDEX;
        }
        
        public void InitForThread(int nativeThreadIndex)
        {
            //TODO: Collections Checks
            LaneIndex = ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex);
            m_AddLaneWriter = m_AddWriter.AsLaneWriter(LaneIndex);
        }

        public void Add(T value)
        {
            //TODO: Collections Checks
            m_AddLaneWriter.Write(ref value);
        }
        
        public void Add(ref T value)
        {
            //TODO: Collections Checks
            m_AddLaneWriter.Write(ref value);
        }
    }

    public readonly struct JobDataForCompletion<T>
        where T : struct
    {
        [ReadOnly] private readonly UnsafeTypedStream<T>.Writer m_CompletionWriter;

        public JobDataForCompletion(UnsafeTypedStream<T>.Writer completionWriter)
        {
            m_CompletionWriter = completionWriter;
        }
        
        public void Add(T value, int laneIndex)
        {
            Add(ref value, laneIndex);
        }
        
        public void Add(ref T value, int laneIndex)
        {
            m_CompletionWriter.AsLaneWriter(laneIndex).Write(ref value);
        }
    }


    public struct JobDataForWork<T>
        where T : struct
    {
        private const int DEFAULT_LANE_INDEX = -1;

        [ReadOnly] private readonly UnsafeTypedStream<T>.Writer m_ContinueWriter;
        [ReadOnly] private readonly NativeArray<T> m_Current;

        private UnsafeTypedStream<T>.LaneWriter m_ContinueLaneWriter;

        public int LaneIndex
        {
            get;
            private set;
        }

        public JobDataForWork(UnsafeTypedStream<T>.Writer continueWriter,
                              NativeArray<T> current)
        {
            m_ContinueWriter = continueWriter;
            m_Current = current;

            m_ContinueLaneWriter = default;
            LaneIndex = DEFAULT_LANE_INDEX;
        }

        public void InitForThread(int nativeThreadIndex)
        {
            //TODO: Collections Checks
            LaneIndex = ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex);
            m_ContinueLaneWriter = m_ContinueWriter.AsLaneWriter(LaneIndex);
        }

        public T this[int index]
        {
            //TODO: Collections Checks
            get => m_Current[index];
        }

        internal void Continue(ref T value)
        {
            //TODO: Collections Checks
            m_ContinueLaneWriter.Write(ref value);
        }
    }
}
