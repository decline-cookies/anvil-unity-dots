using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public enum JobDataContext
    {
        Read,
        Add,
        Update,
        ResultsDestination
    }

    public class JobData
    {
        private class ReferencedData
        {
            private delegate JobHandle AcquireDelegate();

            private delegate void ReleaseDelegate(JobHandle releaseAccessDependency);

            public JobDataContext Context
            {
                get;
            }

            public IVirtualData Data
            {
                get;
            }

            private readonly AcquireDelegate m_AcquireDelegate;
            private readonly ReleaseDelegate m_ReleaseDelegate;
            private readonly AccessController m_AccessController;


            public ReferencedData(IVirtualData data, JobDataContext context)
            {
                Data = data;
                m_AccessController = Data.AccessController;
                Context = context;
                m_AcquireDelegate = context switch
                {
                    JobDataContext.Read               => AcquireForReadAsync,
                    JobDataContext.Add                => AcquireForSharedWriteAsync,
                    JobDataContext.Update             => AcquireForUpdate,
                    JobDataContext.ResultsDestination => AcquireForResultsDestination,
                    _                                 => throw new ArgumentOutOfRangeException(nameof(context), context, null)
                };

                m_ReleaseDelegate = context switch
                {
                    JobDataContext.Read               => ReleaseAsync,
                    JobDataContext.Add                => ReleaseAsync,
                    JobDataContext.Update             => ReleaseAsync,
                    JobDataContext.ResultsDestination => ReleaseResultsDestination,
                    _                                 => throw new ArgumentOutOfRangeException(nameof(context), context, null)
                };
            }

            private JobHandle AcquireForReadAsync()
            {
                return m_AccessController.AcquireAsync(AccessType.SharedRead);
            }

            private JobHandle AcquireForSharedWriteAsync()
            {
                return m_AccessController.AcquireAsync(AccessType.SharedWrite);
            }

            private JobHandle AcquireForUpdate()
            {
                return Data.AcquireForUpdate();
            }

            private JobHandle AcquireForResultsDestination()
            {
                return default;
            }

            private void ReleaseAsync(JobHandle releaseAccessDependency)
            {
                Data.ReleaseForUpdate(releaseAccessDependency);
            }

            private void ReleaseResultsDestination(JobHandle releaseAccessDependency)
            {
                //Does Nothing
            }

            public JobHandle Acquire()
            {
                return m_AcquireDelegate();
            }

            public void Release(JobHandle releaseAccessDependency)
            {
                m_ReleaseDelegate(releaseAccessDependency);
            }
        }

        public delegate JobHandle JobDataDelegate(JobHandle dependsOn, JobData jobData);

        private readonly Dictionary<Type, ReferencedData> m_ReferencedData = new Dictionary<Type, ReferencedData>();
        private readonly JobDataDelegate m_JobDataDelegate;
        private readonly BatchStrategy m_BatchStrategy;

        private IVirtualData m_SchedulingData;

        public AbstractTaskDriverSystem System
        {
            get;
        }

        public World World
        {
            get;
        }

        public ref readonly TimeData Time
        {
            get => ref World.Time;
        }


        internal JobData(JobDataDelegate jobDataDelegate, BatchStrategy batchStrategy, AbstractTaskDriverSystem abstractTaskDriverSystem)
        {
            m_JobDataDelegate = jobDataDelegate;
            m_BatchStrategy = batchStrategy;
            System = abstractTaskDriverSystem;
            World = System.World;
        }

        public JobData AddRequiredData<TKey, TInstance>(VirtualData<TKey, TInstance> data, JobDataContext context, bool shouldBeUsedForScheduling = false)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            m_ReferencedData.Add(typeof(VirtualData<TKey, TInstance>), new ReferencedData(data, context));

            //TODO: Exceptions
            if (shouldBeUsedForScheduling)
            {
                BatchSize = m_BatchStrategy == BatchStrategy.MaximizeChunk
                    ? VirtualData<TKey, TInstance>.MAX_ELEMENTS_PER_CHUNK
                    : 1;
                m_SchedulingData = data;
            }

            return this;
        }

        //*************************************************************************************************************
        // Update
        //*************************************************************************************************************

        public JobHandle PrepareAndSchedule(JobHandle dependsOn)
        {
            int len = m_ReferencedData.Count;
            if (len == 0)
            {
                return dependsOn;
            }

            NativeArray<JobHandle> dataDependencies = new NativeArray<JobHandle>(len, Allocator.Temp);

            int index = 0;
            foreach (ReferencedData data in m_ReferencedData.Values)
            {
                dataDependencies[index] = data.Acquire();
                index++;
            }

            JobHandle delegateDependency = m_JobDataDelegate(JobHandle.CombineDependencies(dataDependencies), this);

            foreach (ReferencedData data in m_ReferencedData.Values)
            {
                data.Release(delegateDependency);
            }

            return delegateDependency;
        }

        //*************************************************************************************************************
        // Reader
        //*************************************************************************************************************

        public int BatchSize
        {
            get;
            private set;
        }


        public DeferredNativeArray<TInstance> GetArrayForScheduling<TKey, TInstance>()
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            //TODO: Exception
            VirtualData<TKey, TInstance> typedData = (VirtualData<TKey, TInstance>)m_SchedulingData;
            return typedData.ArrayForScheduling;
        }


        public VDJobUpdater<TKey, TInstance> GetUpdater<TKey, TInstance>()
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            //TODO: Exceptions
            VirtualData<TKey, TInstance> typedData = (VirtualData<TKey, TInstance>)m_ReferencedData[typeof(VirtualData<TKey, TInstance>)].Data;
            return typedData.CreateVDJobUpdater();
        }

        public VDJobReader<TInstance> GetReader<TKey, TInstance>()
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            //TODO: Exceptions
            VirtualData<TKey, TInstance> typedData = (VirtualData<TKey, TInstance>)m_ReferencedData[typeof(VirtualData<TKey, TInstance>)].Data;
            return typedData.CreateVDJobReader();
        }

        public VDJobWriter<TInstance> GetWriter<TKey, TInstance>()
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            //TODO: Exceptions
            VirtualData<TKey, TInstance> typedData = (VirtualData<TKey, TInstance>)m_ReferencedData[typeof(VirtualData<TKey, TInstance>)].Data;
            return typedData.CreateVDJobWriter();
        }


        public VDJobResultsDestination<TInstance> GetResultsDestination<TKey, TInstance>()
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            //TODO: Exceptions
            VirtualData<TKey, TInstance> typedData = (VirtualData<TKey, TInstance>)m_ReferencedData[typeof(VirtualData<TKey, TInstance>)].Data;
            return typedData.CreateVDJobResultsDestination();
        }
    }
}
