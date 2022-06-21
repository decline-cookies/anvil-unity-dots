namespace Anvil.Unity.DOTS.Data
{
    public interface IVirtualDataInstance<TResult>
        where TResult : struct
    {
        public VDJobResultsDestination<TResult> ResultsDestination
        {
            get;
        }
    }
}
