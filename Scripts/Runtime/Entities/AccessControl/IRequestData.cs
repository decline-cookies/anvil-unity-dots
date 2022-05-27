namespace Anvil.Unity.DOTS.Entities
{
    public interface IRequestData<TResponse>
        where TResponse : struct
    {
        public ResponseJobData<TResponse> ResponseWriter
        {
            get;
        }
    }
}
