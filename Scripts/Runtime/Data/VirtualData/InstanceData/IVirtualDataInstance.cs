namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// Data to be used in <see cref="VirtualData{TKey,TInstance}"/>
    /// that can write out a result when it has completed updating.
    /// </summary>
    /// <typeparam name="TResult">The type of result to write</typeparam>
    public interface IVirtualDataInstance<TResult>
        where TResult : struct
    {
        /// <summary>
        /// The location to write the result
        /// </summary>
        public VDJobResultsDestination<TResult> ResultsDestination
        {
            get;
        }
    }
}
