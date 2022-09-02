using System;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// Helper methods when working with <see cref="ProxyDataStream{TInstance}"/>
    /// These make it more clear what is happening when operating on <see cref="ProxyDataStream{TInstance}"/> instances
    /// in a job.
    /// </summary>
    public static class VirtualDataExtensions
    {
        //TODO: DOCS
        public static void Resolve<TInstance, TResult, TEnum>(this TInstance instance, TEnum option, ref TResult result, ref VDUpdater<TInstance> updater)
            where TInstance : unmanaged, IEntityProxyData, ITaskData
            where TResult : unmanaged, IEntityProxyData
            where TEnum : unmanaged, Enum
        {
            VDResultsDestination<TResult> resultsDestination = instance.ResultsDestinationLookup.GetVDResultsDestination<TEnum, TResult>(option);
            VDResultsWriter<TResult> resultsWriter = resultsDestination.AsResultsWriter(updater.CurrentContext);
            resultsWriter.Add(ref result, updater.LaneIndex);
            updater.Resolve();
        }
        
        //TODO: DOCS
        public static void Resolve<TInstance, TResult, TEnum>(this TInstance instance, TEnum option, TResult result, ref VDUpdater<TInstance> updater)
            where TInstance : unmanaged, IEntityProxyData, ITaskData
            where TResult : unmanaged, IEntityProxyData
            where TEnum : unmanaged, Enum
        {
            Resolve(instance, option, ref result, ref updater);
        }

        //TODO: DOCS
        public static void Resolve<TInstance>(this TInstance instance, ref VDUpdater<TInstance> updater)
            where TInstance : unmanaged, IEntityProxyData
        {
            updater.Resolve();
        }
        
        //TODO: DOCS
        public static void ContinueOn<TInstance>(this TInstance instance, ref VDUpdater<TInstance> updater)
            where TInstance : unmanaged, IEntityProxyData
        {
            updater.Continue(ref instance);
        }
    }
}
