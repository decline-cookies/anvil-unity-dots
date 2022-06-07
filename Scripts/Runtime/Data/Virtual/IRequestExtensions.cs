namespace Anvil.Unity.DOTS.Data
{
    public static class IRequestExtensions
    {
        public static void Complete<TRequest, TResponse>(this TRequest request, ref TResponse response, ref JobDataForWork<TRequest> jobDataForWork)
            where TResponse : struct
            where TRequest : struct, IRequest<TResponse>
        { 
            request.ResponseWriter.Add(response, jobDataForWork.LaneIndex);
        }
        
        public static void Complete<TRequest, TResponse>(this TRequest request, TResponse response, ref JobDataForWork<TRequest> jobDataForWork)
            where TResponse : struct
            where TRequest : struct, IRequest<TResponse>
        { 
            request.ResponseWriter.Add(response, jobDataForWork.LaneIndex);
        }

        public static void Continue<TRequest>(this TRequest request, ref JobDataForWork<TRequest> jobDataForWork)
            where TRequest : struct
        {
            jobDataForWork.Continue(ref request);
        }
    }
}
