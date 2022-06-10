namespace Anvil.Unity.DOTS.Data
{
    public interface ISourceData<TResult>
        where TResult : struct
    {
        public JobResultWriter<TResult> ResultWriter
        {
            get;
        }
    }
}
