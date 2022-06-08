using System;

namespace Anvil.Unity.DOTS.Data
{
    public static class IRequestExtensions
    {
        //TODO: Docs and sort out ref's nicely
        
        public static void Complete<TRequest, TResponse>(this TRequest request, ref TResponse response, ref JobDataForWork<TRequest> jobDataForWork)
            where TResponse : struct
            where TRequest : struct, IRequest<TResponse>
        { 
            request.ResponseWriter.Add(response, jobDataForWork.LaneIndex);
        }
        
        public static void Complete<TRequest, TResponse>(this TRequest request, TResponse response, ref JobDataForWork<TRequest> jobDataForWork)
            where TResponse : struct
            where TRequest : struct, IRequest<TResponse>
        { 
            request.ResponseWriter.Add(response, jobDataForWork.LaneIndex);
        }

        public static void CompleteAndRemove<TKey, TValue, TResponse>(this TValue value, TResponse response, ref LookupJobDataForWork<TKey, TValue> lookupJobDataForWork)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct, ILookupValue<TKey>, IRequest<TResponse>
            where TResponse : struct
        {
            value.ResponseWriter.Add(response, lookupJobDataForWork.LaneIndex);
            lookupJobDataForWork.Remove(value.Key);
        }

        public static void ContinueIn<TRequest>(this TRequest request, ref JobDataForWork<TRequest> jobDataForWork)
            where TRequest : struct
        {
            jobDataForWork.Continue(ref request);
        }

        public static void RemoveFrom<TKey, TValue>(this TValue value, ref LookupJobDataForWork<TKey, TValue> lookupJobDataForWork)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct, ILookupValue<TKey>
        {
            lookupJobDataForWork.Remove(value.Key);
        }
        
        public static void RemoveFrom<TKey, TValue>(this TKey key, ref LookupJobDataForWork<TKey, TValue> lookupJobDataForWork)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct, ILookupValue<TKey>
        {
            lookupJobDataForWork.Remove(key);
        }
        
        public static void RemoveFrom<TKey, TValue>(this TValue value, ref LookupJobDataForExternalWork<TKey, TValue> lookupJobDataForWork)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct, ILookupValue<TKey>
        {
            lookupJobDataForWork.Remove(value.Key);
        }
        
        public static void RemoveFrom<TKey, TValue>(this TKey key, ref LookupJobDataForExternalWork<TKey, TValue> lookupJobDataForWork)
            where TKey : struct, IEquatable<TKey>
            where TValue : struct, ILookupValue<TKey>
        {
            lookupJobDataForWork.Remove(key);
        }
    }
}
