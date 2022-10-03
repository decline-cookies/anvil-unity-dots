using Anvil.CSharp.Core;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal abstract class AbstractEntityQueryNativeArray : AbstractAnvilBase
    {
        public abstract int Length { get; }

        protected EntityQuery EntityQuery { get; }

        protected AbstractEntityQueryNativeArray(EntityQuery entityQuery)
        {
            EntityQuery = entityQuery;
        }
        
        public abstract JobHandle Acquire();
        public abstract void Release(JobHandle releaseAccessDependency);
    }
}
