using System;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [Flags]
    internal enum TaskStreamFlags
    {
        Default = 0,
        IsResolveTarget = 1 << 0,
        IsCancellable = 1 << 1
    }
}
