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
        public static void Resolve<TInstance, TResult, TEnum>(this TInstance instance, TEnum option, ref TResult result, ref VDUpdater<TInstance> updater)
            where TInstance : unmanaged, IKeyedData, ITaskData
            where TResult : unmanaged, IKeyedData
            where TEnum : Enum
        {
            VDResultsDestination<TResult> resultsDestination = instance.ResultsDestinationLookup.GetVDResultsDestination<TEnum, TResult>(option);
            VDResultsWriter<TResult> resultsWriter = resultsDestination.AsResultsWriter(updater.CurrentContext);
            resultsWriter.Add(ref result, updater.LaneIndex);
            updater.Resolve();
        }
        
        //TODO: DOCS
        public static void Resolve<TInstance, TResult, TEnum>(this TInstance instance, TEnum option, TResult result, ref VDUpdater<TInstance> updater)
            where TInstance : unmanaged, IKeyedData, ITaskData
            where TResult : unmanaged, IKeyedData
            where TEnum : Enum
        {
            Resolve(instance, option, ref result, ref updater);
        }

        //TODO: DOCS
        public static void Resolve<TInstance>(this TInstance instance, ref VDUpdater<TInstance> updater)
            where TInstance : unmanaged, IKeyedData
        {
            updater.Resolve();
        }
        
        //TODO: DOCS
        public static void ContinueOn<TInstance>(this TInstance instance, ref VDUpdater<TInstance> updater)
            where TInstance : unmanaged, IKeyedData
        {
            updater.Continue(ref instance);
        }
    }
}
