using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal interface IAccessControlledDataStream : IAnvilDisposable
    {
        public AccessController AccessController { get; }
    }
}
