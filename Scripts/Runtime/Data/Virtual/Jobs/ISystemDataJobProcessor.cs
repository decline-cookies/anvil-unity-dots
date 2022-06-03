namespace Anvil.Unity.DOTS.Data
{
    public interface ISystemDataJobProcessor<TRequest, TResponse> : ISystemDataJobReader<TRequest>
        where TRequest : struct, IRequest<TResponse>
        where TResponse : struct
    {
        void Complete(ref TRequest request, ref TResponse response);
    }
}