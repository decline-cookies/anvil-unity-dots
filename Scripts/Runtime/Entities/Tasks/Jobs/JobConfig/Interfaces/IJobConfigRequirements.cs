using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Represents a configuration object for a Job that will be run through the Task system.
    /// Extending on <see cref="IJobConfig"/> it allows for requiring certain aspects of data for read or write.
    /// </summary>
    public interface IJobConfigRequirements : IJobConfig
    {
        /// <summary>
        /// Specifies a <see cref="TaskStream{TInstance}"/> to be written to in a shared-write context.
        /// </summary>
        /// <param name="taskStream">The <see cref="TaskStream{TInstance}"/> to write to.</param>
        /// <typeparam name="TInstance">The type of <see cref="IEntityProxyInstance"/> data in
        /// the <see cref="TaskStream{TInstance}"/></typeparam>
        /// <returns>Reference to itself to continue chaining configuration methods</returns>
        public IJobConfigRequirements RequireTaskStreamForWrite<TInstance>(TaskStream<TInstance> taskStream)
            where TInstance : unmanaged, IEntityProxyInstance;
        
        /// <summary>
        /// Specifies a <see cref="TaskStream{TInstance}"/> to be read from in a shared-read context.
        /// </summary>
        /// <param name="taskStream">The <see cref="TaskStream{TInstance}"/> to read from.</param>
        /// <typeparam name="TInstance">The type of <see cref="IEntityProxyInstance"/> data in
        /// the <see cref="TaskStream{TInstance}"/></typeparam>
        /// <returns>Reference to itself to continue chaining configuration methods</returns>
        public IJobConfigRequirements RequireTaskStreamForRead<TInstance>(TaskStream<TInstance> taskStream)
            where TInstance : unmanaged, IEntityProxyInstance;
        
        /// <summary>
        /// Specifies a <see cref="NativeArray{T}"/> to be read from in a shared-read context.
        /// </summary>
        /// <param name="array">The <see cref="NativeArray{T}"/> to read from.</param>
        /// <typeparam name="T">The struct inside the <see cref="NativeArray{T}"/></typeparam>
        /// <remarks>
        /// WARNING
        /// There is no guarantee that this NativeArray is in a state that it can be safely read from.
        /// You must manage access yourself outside of the Task system and ensure it is free from race conditions.
        /// In most cases it is better to use a <see cref="TaskStream{TInstance}"/> or <see cref="EntityQuery"/>.
        /// Use of a <see cref="NativeArray{T}"/> like this is generally used for one time population jobs in very
        /// controlled circumstances.
        /// </remarks>
        /// <returns>Reference to itself to continue chaining configuration methods</returns>
        public IJobConfigRequirements RequireNativeArrayForRead<T>(NativeArray<T> array)
            where T : struct;
        
        /// <summary>
        /// Specifies an <see cref="EntityQuery"/> to be transformed into a <see cref="NativeArray{Entity}"/> and read
        /// from in a shared-read context.
        /// </summary>
        /// <param name="entityQuery">The <see cref="EntityQuery"/> to use.</param>
        /// <remarks>
        /// WARNING
        /// If the <see cref="EntityQuery"/> was not created in a System
        /// via <see cref="ComponentSystemBase.GetEntityQuery"/> there is no guarantee that you will have access to
        /// read the data from the <see cref="EntityQuery"/> as it will not be hooked into Unity's dependency system.
        /// Please ensure you create all <see cref="EntityQuery"/>s for use with the Task system via
        /// <see cref="ComponentSystemBase.GetEntityQuery"/>.
        /// </remarks>
        /// <returns>Reference to itself to continue chaining configuration methods</returns>
        public IJobConfigRequirements RequireEntityNativeArrayFromQueryForRead(EntityQuery entityQuery);

        /// <summary>
        /// Specifies an <see cref="EntityQuery"/> to be transformed into a <see cref="NativeArray{T}"/> and read
        /// from in a shared-read context.
        /// </summary>
        /// <param name="entityQuery">The <see cref="EntityQuery"/> to use.</param>
        /// <typeparam name="T">The type of <see cref="IComponentData"/> to get from the query.</typeparam>
        /// <remarks>
        /// WARNING
        /// If the <see cref="EntityQuery"/> was not created in a System
        /// via <see cref="ComponentSystemBase.GetEntityQuery"/> there is no guarantee that you will have access to
        /// read the data from the <see cref="EntityQuery"/> as it will not be hooked into Unity's dependency system.
        /// Please ensure you create all <see cref="EntityQuery"/>s for use with the Task system via
        /// <see cref="ComponentSystemBase.GetEntityQuery"/>.
        /// </remarks>
        /// <returns>Reference to itself to continue chaining configuration methods</returns>
        public IJobConfigRequirements RequireIComponentDataNativeArrayFromQueryForRead<T>(EntityQuery entityQuery)
            where T : struct, IComponentData;
        
        /// <summary>
        /// Specifies an <see cref="AbstractTaskDriver"/> that can have instances of data cancelled.
        /// </summary>
        /// <param name="taskDriver">The <see cref="AbstractTaskDriver"/> to allow for cancelling</param>
        /// <returns>Reference to itself to continue chaining configuration methods</returns>
        public IJobConfigRequirements RequireTaskDriverForRequestCancel(AbstractTaskDriver taskDriver);
    }
}
