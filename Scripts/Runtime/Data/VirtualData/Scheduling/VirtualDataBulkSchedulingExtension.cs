using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Data
{
    internal static class VirtualDataBulkSchedulingExtension
    {
        internal static JobHandle BulkScheduleParallel<TDictionaryKey, TElement, TKey>(this Dictionary<TDictionaryKey, TElement>.ValueCollection valueCollection,
                                                                                       JobHandle dependsOn,
                                                                                       CancelVirtualData<TKey> cancelData,
                                                                                       VirtualDataBulkScheduleDelegate<TElement, TKey> scheduleFunc)
            where TKey : unmanaged, IEquatable<TKey>
        {
            int len = valueCollection.Count;
            if (len == 0)
            {
                return dependsOn;
            }

            NativeArray<JobHandle> dependencies = new NativeArray<JobHandle>(len, Allocator.Temp);
            int index = 0;
            foreach (TElement element in valueCollection)
            {
                dependencies[index] = scheduleFunc(element, dependsOn, cancelData);
                index++;
            }

            return JobHandle.CombineDependencies(dependencies);
        }
    }
}
