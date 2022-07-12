using System;
using System.Reflection;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// Helper functions for bulk scheduling jobs.
    /// <see cref="BulkScheduleDelegate{T}"/> and <see cref="BulkSchedulingExtension"/>
    /// </summary>
    public static class BulkSchedulingUtil
    {
        /// <summary>
        /// Creates a <see cref="BulkScheduleDelegate{T}"/> easily via reflection. 
        /// </summary>
        /// <remarks>
        /// Best practice would be to call this and store in a static readonly field so the reflection cost only
        /// happens once for the lifetime of the application.
        /// </remarks>
        /// <param name="methodName">The name of the method to call</param>
        /// <param name="bindingFlags">The binding flags of the method</param>
        /// <typeparam name="T">The type the method is found on</typeparam>
        /// <returns>The created <see cref="BulkScheduleDelegate{T}"/></returns>
        public static TDelegate CreateSchedulingDelegate<TDelegate, T>(string methodName, BindingFlags bindingFlags)
            where TDelegate : Delegate
        {
            MethodInfo methodInfo = typeof(T).GetMethod(methodName, bindingFlags);
            if (methodInfo == null)
            {
                throw new InvalidOperationException($"Tried to create a {typeof(TDelegate)} on {typeof(T)} for a method named {methodName} but none exists with the passed binding flags!");
            }
            return (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), methodInfo);
        }
    }
}
