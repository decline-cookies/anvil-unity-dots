namespace Anvil.Unity.DOTS.Data
{
    public interface ICompletable<TCompletionData>
        where TCompletionData : struct
    {
        public JobDataForCompletion<TCompletionData> CompletionWriter
        {
            get;
        }
    }
}
