using Anvil.CSharp.Core;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Represents the untyped version of <see cref="TaskStream{TInstance}"/> for generic internal processing.
    /// </summary>
    public abstract class AbstractTaskStream : AbstractAnvilBase
    {
        internal abstract bool IsCancellable { get; }
        internal abstract bool IsDataStreamAResolveTarget { get; }
        internal abstract AbstractEntityProxyDataStream GetDataStream();
        internal abstract AbstractEntityProxyDataStream GetPendingCancelDataStream();
    }
}
