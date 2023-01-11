using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Represents a configuration object for a Job that will be run through the Task system.
    /// </summary>
    public interface IJobConfig
    {
        /// <summary>
        /// Whether the Job is enabled or not.
        /// A job that is not enabled will not be scheduled or run but will still exist as part of the
        /// <see cref="AbstractTaskDriverSystem"/> or <see cref="AbstractTaskDriver"/> that it is associated with.
        /// </summary>
        public bool IsEnabled { get; set; }
        
        /// <summary>
        /// A configuration helper that will run this job only once.
        /// After being run, it will set <see cref="IsEnabled"/> to false.
        /// </summary>
        /// <remarks>
        /// This is useful for the initial setup jobs.
        /// </remarks>
        /// <returns>A reference to itself to continue chaining configuration methods</returns>
        public IJobConfig RunOnce();
        
        /// <summary>
        /// Specifies a <see cref="IDataStream{TInstance}"/> to be written to in a shared-write context.
        /// </summary>
        /// <param name="dataStream">The <see cref="IDataStream{TInstance}"/> to write to.</param>
        /// <typeparam name="TInstance">The type of <see cref="IEntityProxyInstance"/> data in
        /// the <see cref="IDataStream{TInstance}"/></typeparam>
        /// <returns>A reference to itself to continue chaining configuration methods</returns>
        public IJobConfig RequireDataStreamForWrite<TInstance>(IAbstractDataStream<TInstance> dataStream)
            where TInstance : unmanaged, IEntityProxyInstance;

        /// <summary>
        /// Specifies a <see cref="IAbstractDataStream{TInstance}"/> to be read from in a shared-read context.
        /// </summary>
        /// <param name="dataStream">The <see cref="IAbstractDataStream{TInstance}"/> to read from.</param>
        /// <typeparam name="TInstance">The type of <see cref="IEntityProxyInstance"/> data in
        /// the <see cref="IAbstractDataStream{TInstance}"/></typeparam>
        /// <returns>A reference to itself to continue chaining configuration methods</returns>
        public IJobConfig RequireDataStreamForRead<TInstance>(IAbstractDataStream<TInstance> dataStream)
            where TInstance : unmanaged, IEntityProxyInstance;

        /// <summary>
        /// Specifies a generic struct to be read from in a shared-read context.
        /// </summary>
        /// <param name="data">An <see cref="AccessControlledValue{T}"/> of the data to be read</param>
        /// <typeparam name="TData">The struct inside the <see cref="AccessControlledValue{T}"/></typeparam>
        /// <remarks>
        /// This is generally used to wrap a Native Collection like a <see cref="NativeArray{T}"/> or other collection
        /// for use in your job.
        /// </remarks>
        /// <returns>A reference to itself to continue chaining configuration methods</returns>
        public IJobConfig RequireGenericDataForRead<TData>(AccessControlledValue<TData> data)
            where TData : struct;

        /// <summary>
        /// Specifies a generic struct to be written to in a shared-write context.
        /// Sections of the struct will be written to by different threads at the same time.
        /// </summary>
        /// <param name="data">An <see cref="AccessControlledValue{T}"/> of the data to be written to.</param>
        /// <typeparam name="TData">The struct inside the <see cref="AccessControlledValue{T}"/></typeparam>
        /// <remarks>
        /// This is generally used to wrap a Native Collection like a <see cref="NativeArray{T}"/> or other collection
        /// for use in your job.
        /// </remarks>
        /// <returns>A reference to itself to continue chaining configuration methods</returns>
        public IJobConfig RequireGenericDataForWrite<TData>(AccessControlledValue<TData> data)
            where TData : struct;

        /// <summary>
        /// Specifies a generic struct to be written to in an exclusive-write context.
        /// The entire struct will be written to by only one thread at a time.
        /// </summary>
        /// <param name="data">An <see cref="AccessControlledValue{T}"/> of the data to be written to.</param>
        /// <typeparam name="TData">The struct inside the <see cref="AccessControlledValue{T}"/></typeparam>
        /// <remarks>
        /// This is generally used to wrap a Native Collection like a <see cref="NativeArray{T}"/> or other collection
        /// for use in your job.
        /// </remarks>
        /// <returns>A reference to itself to continue chaining configuration methods</returns>
        public IJobConfig RequireGenericDataForExclusiveWrite<TData>(AccessControlledValue<TData> data)
            where TData : struct;

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
        /// <returns>A reference to itself to continue chaining configuration methods</returns>
        public IJobConfig RequireEntityNativeArrayFromQueryForRead(EntityQuery entityQuery);

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
        /// <returns>A reference to itself to continue chaining configuration methods</returns>
        public IJobConfig RequireIComponentDataNativeArrayFromQueryForRead<T>(EntityQuery entityQuery)
            where T : struct, IComponentData;

        public IJobConfig RequestCancelFor(AbstractTaskDriver taskDriver);

        /// <summary>
        /// Specifies a <see cref="ComponentDataFromEntity{T}"/> to be read from in a shared-read context.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IComponentData"/> in the CDFE</typeparam>
        /// <returns>A reference to itself to continue chaining configuration methods</returns>
        public IJobConfig RequireCDFEForRead<T>()
            where T : struct, IComponentData;

        /// <summary>
        /// Specifies a <see cref="ComponentDataFromEntity{T}"/> to be written to in a shared-write context.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IComponentData"/> in the CDFE</typeparam>
        /// <returns>A reference to itself to continue chaining configuration methods</returns>
        public IJobConfig RequireCDFEForWrite<T>()
            where T : struct, IComponentData;

        /// <summary>
        /// Specifies a <see cref="BufferFromEntity{T}"/> to be read from in a shared-read context.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IBufferElementData"/> in the DBFE</typeparam>
        /// <returns>A reference to itself to continue chaining configuration methods</returns>
        public IJobConfig RequireDBFEForRead<T>()
            where T : struct, IBufferElementData;

        /// <summary>
        /// Specifies a <see cref="BufferFromEntity{T}"/> to be written to in an exclusive-write context.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IBufferElementData"/> in the DBFE</typeparam>
        /// <returns>A reference to itself to continue chaining configuration methods</returns>
        public IJobConfig RequireDBFEForExclusiveWrite<T>()
            where T : struct, IBufferElementData;
    }
}