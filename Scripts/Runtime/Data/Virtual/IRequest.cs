namespace Anvil.Unity.DOTS.Data
{
    public interface IRequest<TResponse>
        where TResponse : struct
    {
        public ResponseJobWriter<TResponse> ResponseWriter
        {
            get;
        }
    }
}
