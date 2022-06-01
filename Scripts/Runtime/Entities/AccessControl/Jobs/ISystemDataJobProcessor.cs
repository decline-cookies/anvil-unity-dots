namespace Anvil.Unity.DOTS.Entities
{
    public interface ISystemDataJobProcessor<TRequest, TResponse> : ISystemDataJobReader<TRequest>
        where TRequest : struct, IRequestData<TResponse>
        where TResponse : struct
    {
        void Complete(ref TRequest request, ref TResponse response);
    }
}
