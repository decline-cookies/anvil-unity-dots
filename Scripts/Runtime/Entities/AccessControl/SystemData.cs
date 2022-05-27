using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

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
    
    public class SystemData<T> : AbstractAnvilBase,
                                 IDataRequest<T>,
                                 IDataResponse<T>,
                                 IDataOwner<T>,
                                 IDataSchedulable<T>
        where T : struct, ICompleteData<T>
    {
        private readonly AccessController m_AccessController;
        private readonly HashSet<IDataRequest<T>> m_RequestSources;
        private readonly HashSet<IDataResponse<T>> m_ResponseChannels;
        
        private UnsafeTypedStream<T> m_Pending;
        private DeferredNativeArray<T> m_Current;
        
        public SystemData()
        {
            m_AccessController = new AccessController();
            m_RequestSources = new HashSet<IDataRequest<T>>();
            m_ResponseChannels = new HashSet<IDataResponse<T>>();
            m_Pending = new UnsafeTypedStream<T>(Allocator.Persistent, Allocator.TempJob);

            BatchSize = ChunkUtil.MaxElementsPerChunk<T>();
        }

        protected override void DisposeSelf()
        {
            foreach (IDataRequest<T> dataRequest in m_RequestSources)
            {
                dataRequest.UnregisterResponseChannel(this);
            }

            m_RequestSources.Clear();
            m_ResponseChannels.Clear();
            m_AccessController.Dispose();
            m_Pending.Dispose();
            m_Current.Dispose();

            base.DisposeSelf();
        }

        //*************************************************************************************************************
        // IDataSchedulable
        //*************************************************************************************************************
        
        public DeferredNativeArray<T> ArrayForScheduling
        {
            get => m_Current;
        }

        public int BatchSize
        {
            get;
        }
        
        //*************************************************************************************************************
        // IDataRequest
        //*************************************************************************************************************

        public void RegisterResponseChannel(IDataResponse<T> responseChannel)
        {
            m_ResponseChannels.Add(responseChannel);
        }

        public void UnregisterResponseChannel(IDataResponse<T> responseChannel)
        {
            m_ResponseChannels.Remove(responseChannel);
        }
        
        public JobHandle AcquireForNew(out DataRequestJobStruct<T> jobData)
        {
            jobData = new DataRequestJobStruct<T>(m_Pending.AsWriter());
            return m_AccessController.AcquireAsync(AccessType.SharedWrite);
        }

        public void ReleaseForNew(JobHandle releaseAccessDependency)
        {
            m_AccessController.ReleaseAsync(releaseAccessDependency);
        }

        //*************************************************************************************************************
        // IDataResponse
        //*************************************************************************************************************
        
        public JobHandle AcquireForResponse()
        {
            return m_AccessController.AcquireAsync(AccessType.SharedWrite);
        }

        public void ReleaseForResponse(JobHandle releaseAccessDependency)
        {
            m_AccessController.ReleaseAsync(releaseAccessDependency);
        }
        
        public UnsafeTypedStream<T>.Writer GetResponseChannel()
        {
            return m_Pending.AsWriter();
        }
        
        //*************************************************************************************************************
        // IDataOwner
        //*************************************************************************************************************

        public JobHandle AcquireForUpdate(JobHandle dependsOn, out DataOwnerJobStruct<T> jobData)
        {
            //Get access to be able to write exclusively, we need to update everything
            JobHandle exclusiveWrite = m_AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            
            //Create a new DeferredNativeArray to hold everything we need this frame
            //TODO: Investigate reusing a DeferredNativeArray
            m_Current = new DeferredNativeArray<T>(Allocator.TempJob);
            
            //Consolidate everything in pending into current so it can be balanced across threads
            ConsolidateToNativeArrayJob<T> consolidateJob = new ConsolidateToNativeArrayJob<T>(m_Pending.AsReader(),
                                                                                               m_Current);
            JobHandle consolidateHandle = consolidateJob.Schedule(JobHandle.CombineDependencies(dependsOn, exclusiveWrite));
            
            //Clear pending so we can use it again
            JobHandle clearHandle = m_Pending.Clear(consolidateHandle);
            
            //Create the job struct to be used by whoever is processing the data
            jobData = new DataOwnerJobStruct<T>(m_Current.AsDeferredJobArray(),
                                                m_Pending.AsWriter());
            
            //If we have any channels that we might be writing responses out to, we need make sure we get access to them
            return AcquireResponseChannelsForUpdate(clearHandle);
        }

        private JobHandle AcquireResponseChannelsForUpdate(JobHandle dependsOn)
        {
            if (m_ResponseChannels.Count == 0)
            {
                return dependsOn;
            }
            
            //Get write access to all possible channels that we can write a response to.
            //+1 to include our incoming dependency
            NativeArray<JobHandle> allDependencies = new NativeArray<JobHandle>(m_ResponseChannels.Count + 1, Allocator.Temp);
            allDependencies[0] = dependsOn;
            int index = 1;
            foreach (IDataResponse<T> responseChannel in m_ResponseChannels)
            {
                allDependencies[index] = responseChannel.AcquireForResponse();
                index++;
            }
                
            return JobHandle.CombineDependencies(allDependencies);
        }
        
        public void ReleaseForUpdate(JobHandle releaseAccessDependency)
        {
            //The native array of current values has been read from this frame, we can dispose it.
            //TODO: Look at clearing instead.
            m_Current.Dispose(releaseAccessDependency);
            //Others can use this again
            m_AccessController.ReleaseAsync(releaseAccessDependency);
            //Release all response channels as well
            ReleaseResponseChannelsForUpdate(releaseAccessDependency);
        }

        private void ReleaseResponseChannelsForUpdate(JobHandle releaseAccessDependency)
        {
            if (m_ResponseChannels.Count == 0)
            {
                return;
            }
            
            foreach (IDataResponse<T> responseChannel in m_ResponseChannels)
            {
                responseChannel.ReleaseForResponse(releaseAccessDependency);
            }
        }
    }
}
