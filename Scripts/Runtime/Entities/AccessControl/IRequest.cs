namespace Anvil.Unity.DOTS.Entities
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
