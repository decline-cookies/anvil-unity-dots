using Anvil.Unity.DOTS.Data;
using System;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal abstract class AbstractVDWrapper : IDataWrapper
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

        public abstract JobHandle Acquire();
        public abstract void Release(JobHandle releaseAccessDependency);
    }
}
