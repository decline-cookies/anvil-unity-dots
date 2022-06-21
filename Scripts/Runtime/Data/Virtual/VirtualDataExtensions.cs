using System;

namespace Anvil.Unity.DOTS.Data
{
    public static class VirtualDataExtensions
    {
        //TODO: Docs and sort out ref's nicely

        public static void Complete<TKey, TInstance, TResult>(this TInstance instance, TResult result, ref VDJobUpdater<TKey, TInstance> jobInstanceUpdater)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>, IVirtualDataInstance<TResult>
            where TResult : struct
        {
            VDJobResultsWriter<TResult> resultsWriter = instance.ResultsDestination;
            resultsWriter.Add(result, jobInstanceUpdater.LaneIndex);
            jobInstanceUpdater.Complete();
        }

        public static void Complete<TKey, TInstance>(this TInstance value, ref VDJobUpdater<TKey, TInstance> jobInstanceUpdater)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            jobInstanceUpdater.Complete();
        }

        public static void ContinueIn<TKey, TInstance>(this TInstance value, ref VDJobUpdater<TKey, TInstance> jobInstanceUpdater)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            jobInstanceUpdater.Continue(ref value);
        }
    }
}
