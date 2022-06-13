using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Data;
using System;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract class AbstractPopulator<TKey, TSource, TResult> : AbstractPopulator
        where TKey : struct, IEquatable<TKey>
        where TSource : struct, ILookupValue<TKey>
        where TResult : struct, ILookupValue<TKey>
    {
        internal abstract JobHandle Populate(JobHandle dependsOn, VirtualData<TKey, TSource> sourceData, VirtualData<TKey, TResult> resultData);
    }

    public abstract class AbstractPopulator : AbstractAnvilBase
    {
        protected World World
        {
            get;
        }
        
        protected AbstractTaskDriverSystem System
        {
            get;
        }
        
        internal AbstractTaskDriver TaskDriver
        {
            get;
            set;
        }
        
    }
}
