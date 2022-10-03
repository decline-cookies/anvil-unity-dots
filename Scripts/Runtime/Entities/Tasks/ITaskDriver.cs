using Anvil.CSharp.Core;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public interface ITaskDriver : IAnvilDisposable
    {
        public byte Context { get; }
        //TODO: Maybe just make an AbstractTaskDriver without generics for this
        internal CancelRequestsDataStream GetCancelRequestsDataStream();

        internal List<CancelRequestsDataStream> GetSubTaskDriverCancelRequests();

        internal ITaskSystem GetTaskSystem();
    }
}
