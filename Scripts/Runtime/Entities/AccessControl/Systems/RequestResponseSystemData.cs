using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IRequestWriter<TRequest>
        where TRequest : struct
    {
        RequestJobWriter<TRequest> AcquireRequestJobWriter();
        void ReleaseRequestJobWriter();

        JobHandle AcquireRequestJobWriterAsync(out RequestJobWriter<TRequest> writer);
        void ReleaseRequestJobWriterAsync(JobHandle releaseAccessDependency);
    }
    
    public class RequestResponseSystemData<TRequest, TResponse> : AbstractSystemData<TRequest>,
                                                                  IRequestWriter<TRequest>
        where TRequest : struct, IRequestData<TResponse>
        where TResponse : struct
    {
        private readonly HashSet<ResponseSystemData<TRequest, TResponse>> m_ResponseChannels;

        public RequestResponseSystemData()
        {
            m_ResponseChannels = new HashSet<ResponseSystemData<TRequest, TResponse>>();
        }

        protected override void DisposeSelf()
        {
            m_ResponseChannels.Clear();
            base.DisposeSelf();
        }


        //*************************************************************************************************************
        // Request
        //*************************************************************************************************************

        public ResponseSystemData<TRequest, TResponse> CreateResponseChannel()
        {
            ResponseSystemData<TRequest, TResponse> responseSystemData = new ResponseSystemData<TRequest, TResponse>(this);
            m_ResponseChannels.Add(responseSystemData);
            return responseSystemData;
        }

        public void UnregisterResponseChannel(ResponseSystemData<TRequest, TResponse> responseSystemData)
        {
            m_ResponseChannels.Remove(responseSystemData);
        }

        public RequestJobWriter<TRequest> AcquireRequestJobWriter()
        {
            //TODO: Collections checks
            UnsafeTypedStream<TRequest>.Writer pendingWriter = AcquirePending(AccessType.ExclusiveWrite);
            return new RequestJobWriter<TRequest>(pendingWriter, true);
        }

        public void ReleaseRequestJobWriter()
        {
            //TODO: Collections checks
            ReleasePending();
        }

        public JobHandle AcquireRequestJobWriterAsync(out RequestJobWriter<TRequest> writer)
        {
            //TODO: Collections checks
            JobHandle handle = AcquirePendingAsync(AccessType.SharedWrite, out UnsafeTypedStream<TRequest>.Writer pendingWriter);
            writer = new RequestJobWriter<TRequest>(pendingWriter);
            return handle;
        }

        public void ReleaseRequestJobWriterAsync(JobHandle releaseAccessDependency)
        {
            //TODO: Collections checks
            ReleasePendingAsync(releaseAccessDependency);
        }


        //*************************************************************************************************************
        // Update
        //*************************************************************************************************************

        public JobHandle AcquireProcessorAsync(JobHandle dependsOn, out RequestResponseJobProcessor<TRequest, TResponse> responseJobProcessor)
        {
            JobHandle dependency = InternalAcquireProcessorAsync(dependsOn);
            //Create the job struct to be used by whoever is processing the data
            responseJobProcessor = new RequestResponseJobProcessor<TRequest, TResponse>(Pending.AsWriter(),
                                                                                        Current.AsDeferredJobArray());
            return dependency;
        }

        protected sealed override JobHandle InternalAcquireProcessorAsync(JobHandle dependsOn)
        {
            JobHandle baseHandle = base.InternalAcquireProcessorAsync(dependsOn);
            //If we have any channels that we might be writing responses out to, we need make sure we get access to them
            return AcquireResponseChannelsForUpdate(baseHandle);
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
            foreach (ResponseSystemData<TRequest, TResponse> responseChannel in m_ResponseChannels)
            {
                allDependencies[index] = responseChannel.AcquireForResponse();
                index++;
            }

            return JobHandle.CombineDependencies(allDependencies);
        }

        public sealed override void ReleaseProcessorAsync(JobHandle releaseAccessDependency)
        {
            base.ReleaseProcessorAsync(releaseAccessDependency);
            //Release all response channels as well
            ReleaseResponseChannelsForUpdate(releaseAccessDependency);
        }

        private void ReleaseResponseChannelsForUpdate(JobHandle releaseAccessDependency)
        {
            if (m_ResponseChannels.Count == 0)
            {
                return;
            }

            foreach (ResponseSystemData<TRequest, TResponse> responseChannel in m_ResponseChannels)
            {
                responseChannel.ReleaseForResponse(releaseAccessDependency);
            }
        }
    }
}
