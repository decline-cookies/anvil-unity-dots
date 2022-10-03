using System;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal interface IAccessWrapper : IDisposable
    {
        public JobHandle Acquire();
        public void Release(JobHandle releaseAccessDependency);
    }
}
