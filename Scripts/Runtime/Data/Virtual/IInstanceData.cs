namespace Anvil.Unity.DOTS.Data
{
    public interface IInstanceData<TResult>
        where TResult : struct
    {
        public JobResultWriter<TResult> ResultWriter
        {
            get;
        }
    }
}
