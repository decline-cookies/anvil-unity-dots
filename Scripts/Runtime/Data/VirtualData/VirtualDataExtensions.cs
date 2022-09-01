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
        //TODO: DOCS
        public static void Resolve<TKey, TInstance, TResult, TEnum>(this TInstance instance, TEnum option, ref TResult result, ref VDUpdater<TKey, TInstance> updater)
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>, ITaskData
            where TResult : unmanaged
            where TEnum : Enum
        {
            VDResultsDestination<TResult> resultsDestination = instance.ResultsDestinationLookup.GetVDResultsDestination<TEnum, TResult>(option);
            VDResultsWriter<TResult> resultsWriter = resultsDestination.AsResultsWriter();
            resultsWriter.Add(ref result, updater.LaneIndex);
            updater.Resolve();
        }
        
        //TODO: DOCS
        public static void Resolve<TKey, TInstance, TResult, TEnum>(this TInstance instance, TEnum option, TResult result, ref VDUpdater<TKey, TInstance> updater)
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>, ITaskData
            where TResult : unmanaged
            where TEnum : Enum
        {
            Resolve(instance, option, ref result, ref updater);
        }

        //TODO: DOCS
        public static void Resolve<TKey, TInstance>(this TInstance instance, ref VDUpdater<TKey, TInstance> updater)
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            updater.Resolve();
        }
        
        //TODO: DOCS
        public static void ContinueOn<TKey, TInstance>(this TInstance instance, ref VDUpdater<TKey, TInstance> updater)
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            updater.Continue(ref instance);
        }
    }
}
