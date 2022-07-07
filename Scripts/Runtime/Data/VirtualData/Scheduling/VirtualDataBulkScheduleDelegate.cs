using System;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Data
{
    internal delegate JobHandle VirtualDataBulkScheduleDelegate<in T, TKey>(T element, JobHandle dependsOn, CancelVirtualData<TKey> cancelData)
        where TKey : unmanaged, IEquatable<TKey>;
}
