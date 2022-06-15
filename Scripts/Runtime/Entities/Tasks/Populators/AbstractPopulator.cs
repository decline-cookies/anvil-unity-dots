using Anvil.Unity.DOTS.Data;
using System;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal abstract class AbstractPopulator<TKey, TSource, TResult>
        where TKey : struct, IEquatable<TKey>
        where TSource : struct, ILookupValue<TKey>
        where TResult : struct, ILookupValue<TKey>
    {
        internal abstract JobHandle Populate(JobHandle dependsOn, VirtualData<TKey, TSource> sourceData, VirtualData<TKey, TResult> resultData);
    }
}
