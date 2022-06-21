using System;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// Data to be used in <see cref="VirtualData{TKey,TInstance}"/>
    /// that can be looked up with a <typeparamref name="TKey"/>
    /// </summary>
    /// <typeparam name="TKey">The type of key to use for lookup.</typeparam>
    public interface ILookupData<out TKey>
        where TKey : struct, IEquatable<TKey>
    {
        /// <summary>
        /// The lookup key for this struct
        /// </summary>
        public TKey Key
        {
            get;
        }
    }
}
