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
        /// Writes result data to the <see cref="VDResultsDestination{TResult}"/> on a
        /// <see cref="IVirtualDataInstance{TResult}"/>
        /// </summary>
        /// <param name="instance">The instance to correspond the result to</param>
        /// <param name="result">The result data to write</param>
        /// <param name="updater">The <see cref="VDUpdater{TKey,TInstance}"/> the instance was from.</param>
        /// <typeparam name="TKey">The type of key for the instance.</typeparam>
        /// <typeparam name="TInstance">The type of the instance struct.</typeparam>
        /// <typeparam name="TResult">The type of the result struct</typeparam>
        public static void Complete<TKey, TInstance, TResult>(this TInstance instance, ref TResult result, ref VDUpdater<TKey, TInstance> updater)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>, IVirtualDataInstance<TResult>
            where TResult : struct
        {
            VDResultsWriter<TResult> resultsWriter = instance.ResultsDestination.AsResultsWriter();
            resultsWriter.Add(ref result, updater.LaneIndex);
            updater.Complete();
        }
        
        /// <inheritdoc cref="Complete{TKey,TInstance,TResult}"/>
        public static void Complete<TKey, TInstance, TResult>(this TInstance instance, TResult result, ref VDUpdater<TKey, TInstance> updater)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>, IVirtualDataInstance<TResult>
            where TResult : struct
        {
            Complete(instance, ref result, ref updater);
        }

        /// <summary>
        /// Consistency helper function to correspond to <see cref="Complete{TKey,TInstance,TResult}(TInstance,ref TResult,ref VDUpdater{TKey,TInstance})"/>
        /// when there is no result to write.
        /// Allows for the code to look the same in the jobs and checks safeties when ENABLE_UNITY_COLLECTIONS_CHECKS is enabled.
        /// </summary>
        /// <param name="instance">The instance to operate on</param>
        /// <param name="updater">The <see cref="VDUpdater{TKey,TInstance}"/> the instance was from.</param>
        /// <typeparam name="TKey">The type of key for the instance.</typeparam>
        /// <typeparam name="TInstance">The type of the instance struct.</typeparam>
        public static void Complete<TKey, TInstance>(this TInstance instance, ref VDUpdater<TKey, TInstance> updater)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>
        {
            updater.Complete();
        }
        
        /// <summary>
        /// Companion to <see cref="Complete{TKey,TInstance,TResult}(TInstance,ref TResult,ref VDUpdater{TKey,TInstance})"/>
        /// where the instance is not ready to write it's result and should be updated again the next frame.
        /// </summary>
        /// <param name="instance">The instance to update again next frame</param>
        /// <param name="updater">The <see cref="VDUpdater{TKey,TInstance}"/> the instance was from.</param>
        /// <typeparam name="TKey">The type of key for the instance.</typeparam>
        /// <typeparam name="TInstance">The type of the instance struct.</typeparam>
        public static void ContinueOn<TKey, TInstance>(this TInstance instance, ref VDUpdater<TKey, TInstance> updater)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, IKeyedData<TKey>
        {
            updater.Continue(ref instance);
        }
    }
}
