using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System.Collections.Generic;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public class ResponseSystemData<TRequest, TResponse> : AbstractSystemData<TResponse>
        where TRequest : struct, IRequestData<TResponse>
        where TResponse : struct
    {
        //TODO: Could there ever be more than one?
        private readonly HashSet<RequestSystemData<TRequest, TResponse>> m_RequestSources;
        
        public ResponseSystemData(RequestSystemData<TRequest, TResponse> requestSystemData)
        {
            m_RequestSources = new HashSet<RequestSystemData<TRequest, TResponse>>
            {
                requestSystemData
            };
        }

        protected override void DisposeSelf()
        {
            foreach (RequestSystemData<TRequest, TResponse> dataRequest in m_RequestSources)
            {
                dataRequest.UnregisterResponseChannel(this);
            }

            m_RequestSources.Clear();
            
            base.DisposeSelf();
        }
        
        //*************************************************************************************************************
        // Response
        //*************************************************************************************************************

        public JobHandle AcquireForResponse()
        {
            return AccessController.AcquireAsync(AccessType.SharedWrite);
        }

        public void ReleaseForResponse(JobHandle releaseAccessDependency)
        {
            AccessController.ReleaseAsync(releaseAccessDependency);
        }

        public ResponseJobData<TResponse> GetResponseChannel()
        {
            return new ResponseJobData<TResponse>(Pending.AsWriter());
        }
        
        //*************************************************************************************************************
        // Update
        //*************************************************************************************************************

        public JobHandle AcquireForUpdate(JobHandle dependsOn, out UpdateResponseJobData<TResponse> jobData)
        {
            JobHandle dependency = AcquireForUpdate(dependsOn);
            //Create the job struct to be used by whoever is processing the data
            jobData = new UpdateResponseJobData<TResponse>(Current.AsDeferredJobArray());
            return dependency;
        }
    }
}
