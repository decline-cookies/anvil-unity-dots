using System;

namespace Anvil.Unity.DOTS.Data
{
    public static class IRequestExtensions
    {
        //TODO: Docs and sort out ref's nicely

        public static void Complete<TKey, TValue, TResponse>(this TValue value, TResponse response, ref LookupJobDataForWork<TKey, TValue> lookupJobDataForWork)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct, ILookupValue<TKey>, IRequest<TResponse>
            where TResponse : struct
        {
            value.ResponseWriter.Add(response, lookupJobDataForWork.LaneIndex);
            lookupJobDataForWork.Complete();
        }

        public static void Complete<TKey, TValue>(this TValue value, ref LookupJobDataForWork<TKey, TValue> jobDataForWork)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct, ILookupValue<TKey>
        {
            jobDataForWork.Complete();
        }

        public static void ContinueIn<TKey, TValue>(this TValue value, ref LookupJobDataForWork<TKey, TValue> jobDataForWork)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct, ILookupValue<TKey>
        {
            jobDataForWork.Continue(ref value);
        }
    }
}
