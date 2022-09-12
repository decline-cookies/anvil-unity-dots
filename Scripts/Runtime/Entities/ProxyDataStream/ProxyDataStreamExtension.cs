namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Helper methods when working with <see cref="ProxyDataStream{TInstance}"/>
    /// These make it more clear what is happening when operating on <see cref="ProxyDataStream{TInstance}"/> instances
    /// in a job.
    /// </summary>
    public static class ProxyDataStreamExtension
    {
        //TODO: RE-ENABLE IF NEEDED
        //TODO: DOCS
        // public static void Resolve<TInstance, TResult, TEnum>(this TInstance data, TEnum option, ref TResult result, ref PDSUpdater<TInstance> updater)
        //     where TInstance : unmanaged, IProxyData
        //     where TResult : unmanaged, IProxyData
        //     where TEnum : unmanaged, Enum
        // {
        //     VDResultsDestination<TResult> resultsDestination = data.ResultsDestinationLookup.GetVDResultsDestination<TEnum, TResult>(option);
        //     VDResultsWriter<TResult> resultsWriter = resultsDestination.AsResultsWriter(updater.CurrentContext);
        //     resultsWriter.Add(ref result, updater.LaneIndex);
        //     updater.Resolve();
        // }
        //
        // //TODO: DOCS
        // public static void Resolve<TInstance, TResult, TEnum>(this TInstance data, TEnum option, TResult result, ref PDSUpdater<TInstance> updater)
        //     where TInstance : unmanaged, IProxyData
        //     where TResult : unmanaged, IProxyData
        //     where TEnum : unmanaged, Enum
        // {
        //     Resolve(data, option, ref result, ref updater);
        // }

        //TODO: Code-stink with extension method that doesn't actually use the data - https://github.com/decline-cookies/anvil-unity-dots/pull/57#discussion_r967355560
        //TODO: DOCS
        public static void Resolve<TInstance>(this TInstance instance, ref DataStreamUpdater<TInstance> updater)
            where TInstance : unmanaged, IProxyInstance
        {
            updater.Resolve();
        }

        //TODO: DOCS
        public static void ContinueOn<TInstance>(this TInstance instance, ref DataStreamUpdater<TInstance> updater)
            where TInstance : unmanaged, IProxyInstance
        {
            updater.Continue(ref instance);
        }
    }
}
