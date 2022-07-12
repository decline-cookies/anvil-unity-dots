namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// Data to be used in <see cref="VirtualData{TKey,TInstance}"/>
    /// that can be looked up with a <typeparamref name="TKey"/>
    /// </summary>
    /// <typeparam name="TKey">The type of key to use for lookup.</typeparam>
    public interface IKeyedData
    {
        /// <summary>
        /// The lookup key for this data
        /// </summary>
        public VDContextID ContextID
        {
            get;
            //TODO: DISCUSS - Way to hide this set. Might not be possible.
            set;
        }
    }
}
