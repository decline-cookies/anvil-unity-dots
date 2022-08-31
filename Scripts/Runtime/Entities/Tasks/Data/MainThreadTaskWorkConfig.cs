using Anvil.Unity.DOTS.Data;
using System;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A <see cref="AbstractTaskWorkConfig"/> specific for Main Thread work.
    /// </summary>
    public class MainThreadTaskWorkConfig : AbstractTaskWorkConfig
    {
        internal MainThreadTaskWorkConfig(AbstractTaskDriverSystem abstractTaskDriverSystem, uint context) : base(abstractTaskDriverSystem, context)
        {
        }

        /// <summary>
        /// Call when configuration of the <see cref="MainThreadTaskWorkConfig"/> is complete
        /// and all the data needed should be acquired to operate on.
        /// </summary>
        /// <returns>
        /// A <see cref="TaskWorkData"/> to use for doing the work.
        /// All required data will have proper access.
        /// </returns>
        public TaskWorkData Acquire()
        {
            foreach (AbstractVDWrapper wrapper in DataWrappers)
            {
                wrapper.Acquire();
            }

            Debug_SetConfigurationStateComplete();

            return TaskWorkData;
        }

        internal void Release()
        {
            foreach (AbstractVDWrapper wrapper in DataWrappers)
            {
                wrapper.Release();
            }
        }

        /// <summary>
        /// Specifies an instance of <see cref="VirtualData{TKey,TInstance}"/> that will be used on the main thread
        /// in an Add context. 
        /// </summary>
        /// <param name="data">The <see cref="VirtualData{TKey,TInstance}"/> that requires access.</param>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TInstance">The type of the data</typeparam>
        /// <returns>This <see cref="MainThreadTaskWorkConfig"/> for chaining additional configuration.</returns>
        public MainThreadTaskWorkConfig RequireDataForAdd<TInstance>(VirtualData<TInstance> data)
            where TInstance : unmanaged, IKeyedData
        {
            InternalRequireDataForAdd(data, false);
            return this;
        }

        public MainThreadTaskWorkConfig RequireResultsDestinationLookup<TInstance>(VirtualData<TInstance> data)
            where TInstance : unmanaged, IKeyedData
        {
            InternalRequireResultsDestinationLookup(data, false);
            return this;
        }


        /// <summary>
        /// Specifies and instance of <see cref="VirtualData{TKey,TInstance}"/> that will be used on the main thread in an
        /// Iterate context. 
        /// </summary>
        /// <param name="data">The <see cref="VirtualData{TKey,TInstance}"/> that requires access.</param>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TInstance">The type of the data</typeparam>
        /// <returns>This <see cref="MainThreadTaskWorkConfig"/> for chaining additional configuration.</returns>
        public MainThreadTaskWorkConfig RequireDataForIterate<TInstance>(VirtualData<TInstance> data)
            where TInstance : unmanaged, IKeyedData
        {
            InternalRequireDataForIterate(data, false);
            return this;
        }

        /// <summary>
        /// Specifies and instance of <see cref="VirtualData{TKey,TInstance}"/> that will be used on the main thread in an
        /// Update context. 
        /// </summary>
        /// <param name="data">The <see cref="VirtualData{TKey,TInstance}"/> that requires access.</param>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TInstance">The type of the data</typeparam>
        /// <returns>This <see cref="MainThreadTaskWorkConfig"/> for chaining additional configuration.</returns>
        public MainThreadTaskWorkConfig RequireDataForUpdate<TKey, TInstance>(VirtualData<TInstance> data)
            where TInstance : unmanaged, IKeyedData
        {
            InternalRequireDataForUpdate(data, false);
            return this;
        }

        /// <summary>
        /// Specifies and instance of <see cref="VirtualData{TKey,TInstance}"/> that will be used on the main thread in an
        /// Results Destination context. 
        /// </summary>
        /// <param name="resultData">
        /// The <see cref="VirtualData{TKey,TInstance}"/> to use as a results destination.
        /// NOTE: No access is necessary for this as it is just being used to point to where to write later on.
        /// </param>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <typeparam name="TResult">The type of the result data</typeparam>
        /// <returns>This <see cref="MainThreadTaskWorkConfig"/> for chaining additional configuration.</returns>
        public MainThreadTaskWorkConfig RequireDataAsResultsDestination<TResult>(VirtualData<TResult> resultData)
            where TResult : unmanaged, IKeyedData
        {
            InternalRequireDataAsResultsDestination(resultData, false);
            return this;
        }
    }
}
