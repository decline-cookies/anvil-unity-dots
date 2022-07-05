using Anvil.Unity.DOTS.Data;
using System;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal abstract class AbstractVDWrapper
    {
        public AbstractVirtualData Data
        {
            get;
        }

        public Type Type
        {
            get;
        }

        protected AbstractVDWrapper(AbstractVirtualData data)
        {
            Data = data;
            Type = data.GetType();
        }

        public abstract JobHandle AcquireAsync();
        public abstract void ReleaseAsync(JobHandle releaseAccessDependency);
        public abstract void Acquire();
        public abstract void Release();
    }
}
