using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Data
{
    public interface IResponseVirtualData<TResponse> : IResponseVirtualData
        where TResponse : struct
    {
        ResponseJobWriter<TResponse> GetResponseJobWriter();
    }

    public interface IResponseVirtualData : IAnvilDisposable
    {
    }

    public class ResponseVirtualData<TResponse> : AbstractVirtualData<TResponse>,
                                                  IResponseVirtualData<TResponse>
        where TResponse : struct
    {
        //TODO: Could there ever be more than one?
        private readonly IRequestVirtualData<TResponse> m_RequestSource;

        internal ResponseVirtualData(IRequestVirtualData<TResponse> requestResponseVirtualData)
        {
            m_RequestSource = requestResponseVirtualData;
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

        //TODO: Do we need the jobhandle to be passed in? 
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
