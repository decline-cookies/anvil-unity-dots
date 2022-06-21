using System;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// Helper methods when working with <see cref="VirtualData{TKey,TInstance}"/>
    /// These make it more clear what is happening when operating on <see cref="VirtualData{TKey,TInstance}"/> instances
    /// in a job.
    /// </summary>
    public static class VirtualDataExtensions
    {
        /// <summary>
        /// Writes a result struct to the <see cref="VDJobResultsDestination{TResult}"/> on a
        /// <see cref="IVirtualDataInstance{TResult}"/>
        /// </summary>
        /// <param name="instance">The instance to correspond the result to</param>
        /// <param name="result">The result struct to write</param>
        /// <param name="jobUpdater">The <see cref="VDJobUpdater{TKey,TInstance}"/> the instance was from.</param>
        /// <typeparam name="TKey">The type of key for the instance.</typeparam>
        /// <typeparam name="TInstance">The type of the instance struct.</typeparam>
        /// <typeparam name="TResult">The type of the result struct</typeparam>
        public static void Complete<TKey, TInstance, TResult>(this TInstance instance, ref TResult result, ref VDJobUpdater<TKey, TInstance> jobUpdater)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>, IVirtualDataInstance<TResult>
            where TResult : struct
        {
            VDJobResultsWriter<TResult> resultsWriter = instance.ResultsDestination;
            resultsWriter.Add(ref result, jobUpdater.LaneIndex);
            jobUpdater.Complete();
        }
        
        /// <inheritdoc cref="Complete{TKey,TInstance,TResult}"/>
        public static void Complete<TKey, TInstance, TResult>(this TInstance instance, TResult result, ref VDJobUpdater<TKey, TInstance> jobUpdater)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>, IVirtualDataInstance<TResult>
            where TResult : struct
        {
            Complete(instance, ref result, ref jobUpdater);
        }

        /// <summary>
        /// Consistency helper function to correspond to <see cref="Complete{TKey,TInstance,TResult}(TInstance,ref TResult,ref Anvil.Unity.DOTS.Data.VDJobUpdater{TKey,TInstance})"/>
        /// when there is no result to write.
        /// Allows for the code to look the same in the jobs and checks safeties when ENABLE_UNITY_COLLECTIONS_CHECKS is enabled.
        /// </summary>
        /// <param name="instance">The instance to operate on</param>
        /// <param name="jobUpdater">The <see cref="VDJobUpdater{TKey,TInstance}"/> the instance was from.</param>
        /// <typeparam name="TKey">The type of key for the instance.</typeparam>
        /// <typeparam name="TInstance">The type of the instance struct.</typeparam>
        public static void Complete<TKey, TInstance>(this TInstance instance, ref VDJobUpdater<TKey, TInstance> jobUpdater)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            jobUpdater.Complete();
        }
        
        /// <summary>
        /// Companion to <see cref="Complete{TKey,TInstance,TResult}(TInstance,ref TResult,ref Anvil.Unity.DOTS.Data.VDJobUpdater{TKey,TInstance})"/>
        /// where the instance is not ready to write it's result and should be updated again the next frame.
        /// </summary>
        /// <param name="instance">The instance to update again next frame</param>
        /// <param name="jobUpdater">The <see cref="VDJobUpdater{TKey,TInstance}"/> the instance was from.</param>
        /// <typeparam name="TKey">The type of key for the instance.</typeparam>
        /// <typeparam name="TInstance">The type of the instance struct.</typeparam>
        public static void ContinueOn<TKey, TInstance>(this TInstance instance, ref VDJobUpdater<TKey, TInstance> jobUpdater)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            jobUpdater.Continue(ref instance);
        }
    }
}
