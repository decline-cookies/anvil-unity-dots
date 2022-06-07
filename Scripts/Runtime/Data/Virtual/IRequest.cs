namespace Anvil.Unity.DOTS.Data
{
    public interface IRequest<TResponse>
        where TResponse : struct
    {
        public JobDataForCompletion<TResponse> ResponseWriter
        {
            get;
        }
    }
}
