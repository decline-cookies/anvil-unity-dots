using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// Interface for dealing with <see cref="VirtualData{TKey,TInstance}"/> in a generic way without having
    /// to know the types.
    /// </summary>
    public interface IVirtualData : IAnvilDisposable
    {
        internal void AddResultDestination(IVirtualData resultData);
        internal void RemoveResultDestination(IVirtualData resultData);
        
        internal JobHandle ConsolidateForFrame(JobHandle dependsOn);

        internal JobHandle AcquireForUpdate();
        internal void ReleaseForUpdate(JobHandle releaseAccessDependency);

        internal AccessController AccessController
        {
            get;
        }
    }
}
