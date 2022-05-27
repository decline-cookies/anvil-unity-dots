using Anvil.Unity.DOTS.Jobs;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public class RequestSystemData<TRequest, TResponse> : AbstractSystemData<TRequest>
        where TRequest : struct, IRequestData<TResponse>
        where TResponse : struct
    {
        private readonly HashSet<ResponseSystemData<TRequest, TResponse>> m_ResponseChannels;

        public RequestSystemData()
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

        public JobHandle AcquireRequest(out RequestJobData<TRequest> jobData)
        {
            jobData = new RequestJobData<TRequest>(Pending.AsWriter());
            return AccessController.AcquireAsync(AccessType.SharedWrite);
        }

        public void ReleaseRequest(JobHandle releaseAccessDependency)
        {
            AccessController.ReleaseAsync(releaseAccessDependency);
        }


        //*************************************************************************************************************
        // Update
        //*************************************************************************************************************

        public JobHandle AcquireForUpdate(JobHandle dependsOn, out UpdateRequestJobData<TRequest, TResponse> jobData)
        {
            JobHandle dependency = AcquireForUpdate(dependsOn);
            //Create the job struct to be used by whoever is processing the data
            jobData = new UpdateRequestJobData<TRequest, TResponse>(Current.AsDeferredJobArray(),
                                                                    Pending.AsWriter());
            return dependency;
        }
        
        protected sealed override JobHandle AcquireResponseChannelsForUpdate(JobHandle dependsOn)
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

        protected sealed override void ReleaseResponseChannelsForUpdate(JobHandle releaseAccessDependency)
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
