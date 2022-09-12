namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Helper methods when working with <see cref="ProxyDataStream{TData}"/>
    /// These make it more clear what is happening when operating on <see cref="ProxyDataStream{TData}"/> instances
    /// in a job.
    /// </summary>
    public static class ProxyDataStreamExtension
    {
        //TODO: RE-ENABLE IF NEEDED
        //TODO: DOCS
        // public static void Resolve<TData, TResult, TEnum>(this TData data, TEnum option, ref TResult result, ref PDSUpdater<TData> updater)
        //     where TData : unmanaged, IProxyData
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
        // public static void Resolve<TData, TResult, TEnum>(this TData data, TEnum option, TResult result, ref PDSUpdater<TData> updater)
        //     where TData : unmanaged, IProxyData
        //     where TResult : unmanaged, IProxyData
        //     where TEnum : unmanaged, Enum
        // {
        //     Resolve(data, option, ref result, ref updater);
        // }

        //TODO: Code-stink with extension method that doesn't actually use the data - https://github.com/decline-cookies/anvil-unity-dots/pull/57#discussion_r967355560
        //TODO: DOCS
        public static void Resolve<TData>(this TData data, ref DataStreamUpdater<TData> updater)
            where TData : unmanaged, IProxyData
        {
            updater.Resolve();
        }
        
        //TODO: DOCS
        public static void ContinueOn<TData>(this TData data, ref DataStreamUpdater<TData> updater)
            where TData : unmanaged, IProxyData
        {
            updater.Continue(ref data);
        }
    }
}
