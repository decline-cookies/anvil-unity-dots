using Anvil.Unity.DOTS.Jobs;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Data
{
    public interface IRequestVirtualData<TRequest, TResponse> : IRequestVirtualData<TResponse>
        where TRequest : struct, IRequest<TResponse>
        where TResponse : struct
    {
        RequestJobWriter<TRequest> AcquireRequestJobWriter();
        void ReleaseRequestJobWriter();

        JobHandle AcquireRequestJobWriterAsync(out RequestJobWriter<TRequest> writer);
        void ReleaseRequestJobWriterAsync(JobHandle releaseAccessDependency);
    }

    public interface IRequestVirtualData<TResponse>
        where TResponse : struct
    {
        ResponseVirtualData<TResponse> CreateResponseVirtualData();
        void UnregisterResponseChannel(ResponseVirtualData<TResponse> responseVirtualData);
    }

    public class RequestResponseVirtualData<TRequest, TResponse> : AbstractVirtualData<TRequest>,
                                                                   IRequestVirtualData<TRequest, TResponse>
        where TRequest : struct, IRequest<TResponse>
        where TResponse : struct
    {
        private readonly HashSet<ResponseVirtualData<TResponse>> m_ResponseChannels;

        public RequestResponseVirtualData()
        {
            m_ResponseChannels = new HashSet<ResponseVirtualData<TResponse>>();
        }

        protected override void DisposeSelf()
        {
            m_ResponseChannels.Clear();
            base.DisposeSelf();
        }


        //*************************************************************************************************************
        // IRequestSystemData
        //*************************************************************************************************************

        public ResponseVirtualData<TResponse> CreateResponseVirtualData()
        {
            ResponseVirtualData<TResponse> responseVirtualData = new ResponseVirtualData<TResponse>(this);
            m_ResponseChannels.Add(responseVirtualData);
            return responseVirtualData;
        }

        public void UnregisterResponseChannel(ResponseVirtualData<TResponse> responseVirtualData)
        {
            m_ResponseChannels.Remove(responseVirtualData);
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
        // Owner
        //*************************************************************************************************************

        public JobHandle AcquireProcessorAsync(JobHandle dependsOn, out RequestResponseJobProcessor<TRequest, TResponse> responseJobProcessor)
        {
            //TODO: Collections checks
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
            foreach (ResponseVirtualData<TResponse> responseChannel in m_ResponseChannels)
            {
                allDependencies[index] = responseChannel.AcquireForResponse();
                index++;
            }

            return JobHandle.CombineDependencies(allDependencies);
        }

        public sealed override void ReleaseProcessorAsync(JobHandle releaseAccessDependency)
        {
            //TODO: Collections checks
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

            foreach (ResponseVirtualData<TResponse> responseChannel in m_ResponseChannels)
            {
                responseChannel.ReleaseForResponse(releaseAccessDependency);
            }
        }
    }
}
