using Anvil.CSharp.Core;

namespace Anvil.Unity.DOTS.Entities
{
    public interface ITaskDriver : IAnvilDisposable
    {
        public byte Context { get; }
        internal CancelRequestsDataStream GetCancelRequestsDataStream();
    }
}
