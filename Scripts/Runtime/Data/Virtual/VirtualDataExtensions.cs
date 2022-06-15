using System;

namespace Anvil.Unity.DOTS.Data
{
    public static class VirtualDataExtensions
    {
        //TODO: Docs and sort out ref's nicely

        public static void Complete<TKey, TInstance, TResult>(this TInstance instance, TResult result, ref JobInstanceUpdater<TKey, TInstance> jobInstanceUpdater)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>, IInstanceData<TResult>
            where TResult : struct
        {
            instance.ResultWriter.Add(result, jobInstanceUpdater.LaneIndex);
            jobInstanceUpdater.Complete();
        }

        public static void Complete<TKey, TInstance>(this TInstance value, ref JobInstanceUpdater<TKey, TInstance> jobInstanceUpdater)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            jobInstanceUpdater.Complete();
        }

        public static void ContinueIn<TKey, TInstance>(this TInstance value, ref JobInstanceUpdater<TKey, TInstance> jobInstanceUpdater)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            jobInstanceUpdater.Continue(ref value);
        }
    }
}
