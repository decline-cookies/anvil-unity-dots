using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IResponseSystemData<TRequest, TResponse>
        where TRequest : struct, IRequest<TResponse>
        where TResponse : struct
    {
        ResponseJobWriter<TResponse> GetResponseJobWriter();
    }
    
    public class ResponseSystemData<TRequest, TResponse> : AbstractSystemData<TResponse>,
                                                           IResponseSystemData<TRequest, TResponse>
        where TRequest : struct, IRequest<TResponse>
        where TResponse : struct
    {
        //TODO: Could there ever be more than one?
        private readonly RequestResponseSystemData<TRequest, TResponse> m_RequestSource;
        
        internal ResponseSystemData(RequestResponseSystemData<TRequest, TResponse> requestResponseSystemData)
        {
            m_RequestSource = requestResponseSystemData;
        }

        protected override void DisposeSelf()
        {
            m_RequestSource.UnregisterResponseChannel(this);

            base.DisposeSelf();
        }
        
        //*************************************************************************************************************
        // IResponseSystemData
        //*************************************************************************************************************

        public JobHandle AcquireForResponse()
        {
            return AccessController.AcquireAsync(AccessType.SharedWrite);
        }

        public void ReleaseForResponse(JobHandle releaseAccessDependency)
        {
            AccessController.ReleaseAsync(releaseAccessDependency);
        }

        public ResponseJobWriter<TResponse> GetResponseJobWriter()
        {
            return new ResponseJobWriter<TResponse>(Pending.AsWriter());
        }
        
        //*************************************************************************************************************
        // Owner
        //*************************************************************************************************************

        public JobHandle AcquireResponseJobReaderAsync(JobHandle dependsOn, out ResponseJobReader<TResponse> responseJobReader)
        {
            //TODO: Collections checks
            JobHandle dependency = InternalAcquireProcessorAsync(dependsOn);
            //Create the job struct to be used by whoever is processing the data
            responseJobReader = new ResponseJobReader<TResponse>(Current.AsDeferredJobArray());
            
            return dependency;
        }

        public void ReleaseResponseJobReaderAsync(JobHandle releaseAccessDependency)
        {
            //TODO: Collections checks
            ReleaseProcessorAsync(releaseAccessDependency);
        }
    }
}
