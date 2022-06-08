using System;

namespace Anvil.Unity.DOTS.Data
{
    public interface ILookupValue<out TKey>
        where TKey : struct, IEquatable<TKey>
    {
        public TKey Key
        {
            get;
        }
    }
}
