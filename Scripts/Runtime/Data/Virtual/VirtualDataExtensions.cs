using System;

namespace Anvil.Unity.DOTS.Data
{
    public static class VirtualDataExtensions
    {
        //TODO: Docs and sort out ref's nicely

        public static void Complete<TKey, TValue, TResponse>(this TValue value, TResponse response, ref JobSourceReader<TKey, TValue> jobDataForWork)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct, ILookupValue<TKey>, ISourceData<TResponse>
            where TResponse : struct
        {
            value.ResultWriter.Add(response, jobDataForWork.LaneIndex);
            jobDataForWork.Complete();
        }

        public static void Complete<TKey, TValue>(this TValue value, ref JobSourceReader<TKey, TValue> jobDataForWork)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct, ILookupValue<TKey>
        {
            jobDataForWork.Complete();
        }

        public static void ContinueIn<TKey, TValue>(this TValue value, ref JobSourceReader<TKey, TValue> jobDataForWork)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct, ILookupValue<TKey>
        {
            jobDataForWork.Continue(ref value);
        }
    }
}
