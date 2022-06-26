using Anvil.Unity.DOTS.Data;
using System;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal interface IDataWrapper
    {
        public AbstractVirtualData Data
        {
            get;
        }

        public Type Type
        {
            get;
        }
        
        JobHandle Acquire();
        void Release(JobHandle releaseAccessDependency);
    }
}
