using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Represents a configuration object for a Job that will be run through the Task system.
    /// </summary>
    public interface IJobConfig
    {
        /// <summary>
        /// A delegate that configures the requirements for a job on the provided <see cref="IJobConfig"/>.
        /// The same <see cref="IJobConfig"/> instance should be returned by the method to allow for chaining of
        /// additional requirements.
        /// </summary>
        /// <param name="taskDriver">
        /// The task driver instance that is configuring the job. This may be used to gain access to streams or other
        /// task driver specific references.
        /// </param>
        /// <param name="jobConfig">The job config instance to set requirements on.</param>
        /// <typeparam name="T">The concrete type of the <see cref="AbstractTaskDriver"/>.</typeparam>
        /// <returns>
        /// A reference to the <see cref="IJobConfig"/> instance passed in to continue chaining configuration methods.
        /// </returns>
        public delegate IJobConfig ConfigureJobRequirementsDelegate<in T>(T taskDriver, IJobConfig jobConfig)
            where T : AbstractTaskDriver;

        /// <summary>
        /// Whether the Job is enabled or not.
        /// A job that is not enabled will not be scheduled or run but will still exist as part of the
        /// <see cref="AbstractTaskDriverSystem"/> or <see cref="AbstractTaskDriver"/> that it is associated with.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// A configuration helper that will run this job only once.
        /// It will set <see cref="IsEnabled"/> to true, and then after being run,
        /// it will set <see cref="IsEnabled"/> to false.
        /// </summary>
        /// <remarks>
        /// This is useful for the initial setup jobs or to run once after making some structural changes.
        /// </remarks>
        /// <returns>A reference to itself to continue chaining configuration methods.</returns>
        public IJobConfig RunOnce();

        /// <summary>
        /// Specifies a <see cref="IAbstractDataStream{TInstance}"/> to be written to in a shared-write context.
        /// </summary>
        /// <param name="dataStream">The <see cref="IAbstractDataStream{TInstance}"/> to write to.</param>
        /// <typeparam name="TInstance">
        /// The type of <see cref="IEntityKeyedTask"/> data in the <see cref="IAbstractDataStream{TInstance}"/>.
        /// </typeparam>
        /// <returns>A reference to itself to continue chaining configuration methods</returns>
        public IJobConfig RequireDataStreamForWrite<TInstance>(IAbstractDataStream<TInstance> dataStream)
            where TInstance : unmanaged, IEntityKeyedTask;

        /// <summary>
        /// Specifies a <see cref="IAbstractDataStream{TInstance}"/> to be read from in a shared-read context.
        /// </summary>
        /// <param name="dataStream">The <see cref="IAbstractDataStream{TInstance}"/> to read from.</param>
        /// <typeparam name="TInstance">
        /// The type of <see cref="IEntityKeyedTask"/> data in the <see cref="IAbstractDataStream{TInstance}"/>.
        /// </typeparam>
        /// <returns>A reference to itself to continue chaining configuration methods.</returns>
        public IJobConfig RequireDataStreamForRead<TInstance>(IAbstractDataStream<TInstance> dataStream)
            where TInstance : unmanaged, IEntityKeyedTask;

        /// <summary>
        /// Specifies a generic struct to be read from in a shared-read context.
        /// </summary>
        /// <param name="data">An <see cref="IReadAccessControlledValue{T}"/> of the data to be read.</param>
        /// <typeparam name="TData">The struct inside the <see cref="IReadAccessControlledValue{T}"/>.</typeparam>
        /// <remarks>
        /// This is generally used to wrap a Native Collection like a <see cref="NativeArray{T}"/> or other collection
        /// for use in your job.
        /// </remarks>
        /// <returns>A reference to itself to continue chaining configuration methods.</returns>
        public IJobConfig RequireGenericDataForRead<TData>(IReadAccessControlledValue<TData> data)
            where TData : struct;

        /// <summary>
        /// Specifies a generic struct to be written to in a shared-write context.
        /// Sections of the struct will be written to by different threads at the same time.
        /// </summary>
        /// <param name="data">An <see cref="IAccessControlledValue{T}"/> of the data to be written to.</param>
        /// <typeparam name="TData">The struct inside the <see cref="IAccessControlledValue{T}"/>.</typeparam>
        /// <remarks>
        /// This is generally used to wrap a Native Collection like a <see cref="NativeArray{T}"/> or other collection
        /// for use in your job.
        /// </remarks>
        /// <returns>A reference to itself to continue chaining configuration methods.</returns>
        public IJobConfig RequireGenericDataForSharedWrite<TData>(ISharedWriteAccessControlledValue<TData> data)
            where TData : struct;


        public IJobConfig RequireThreadPersistentDataForWrite<TData>(IThreadPersistentData<TData> threadPersistentData)
            where TData : unmanaged, IThreadPersistentDataInstance;

        public IJobConfig RequireThreadPersistentDataForRead<TData>(IThreadPersistentData<TData> threadPersistentData)
            where TData : unmanaged, IThreadPersistentDataInstance;

        public IJobConfig RequireEntityPersistentDataForWrite<TData>(IEntityPersistentData<TData> entityPersistentData)
            where TData : unmanaged, IEntityPersistentDataInstance;

        public IJobConfig RequireEntityPersistentDataForRead<TData>(IReadOnlyEntityPersistentData<TData> entityPersistentData)
            where TData : unmanaged, IEntityPersistentDataInstance;

        /// <summary>
        /// Specifies a generic struct to be written to in an exclusive-write context.
        /// The entire struct will be written to by only one thread at a time.
        /// </summary>
        /// <param name="data">An <see cref="IAccessControlledValue{T}"/> of the data to be written to.</param>
        /// <typeparam name="TData">The struct inside the <see cref="IAccessControlledValue{T}"/>.</typeparam>
        /// <remarks>
        /// This is generally used to wrap a Native Collection like a <see cref="NativeArray{T}"/> or other collection
        /// for use in your job.
        /// </remarks>
        /// <returns>A reference to itself to continue chaining configuration methods.</returns>
        public IJobConfig RequireGenericDataForExclusiveWrite<TData>(IExclusiveWriteAccessControlledValue<TData> data)
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
        /// <returns>A reference to itself to continue chaining configuration methods.</returns>
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
        /// <returns>A reference to itself to continue chaining configuration methods.</returns>
        public IJobConfig RequireIComponentDataNativeArrayFromQueryForRead<T>(EntityQuery entityQuery)
            where T : struct, IComponentData;

        /// <summary>
        /// Requests cancellation for specific <see cref="Entity"/> in a given <see cref="AbstractTaskDriver"/>.
        /// </summary>
        /// <param name="taskDriver">The <see cref="AbstractTaskDriver"/> to cancel.</param>
        /// <returns>A reference to itself to continue chaining configuration methods.</returns>
        public IJobConfig RequestCancelFor(AbstractTaskDriver taskDriver);
        
       
        /// <summary>
        /// Requires an <see cref="EntitySpawner"/> from a given <see cref="EntitySpawnSystem"/>
        /// </summary>
        /// <param name="entitySpawnSystem">The <see cref="EntitySpawnSystem"/> to acquire from.</param>
        /// <returns>A reference to itself to continue chaining configuration methods</returns>
        public IJobConfig RequireEntitySpawner(EntitySpawnSystem entitySpawnSystem);

        /// <summary>
        /// Specifies a <see cref="ComponentDataFromEntity{T}"/> to be read from in a shared-read context.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IComponentData"/> in the CDFE.</typeparam>
        /// <returns>A reference to itself to continue chaining configuration methods.</returns>
        public IJobConfig RequireCDFEForRead<T>()
            where T : struct, IComponentData;

        /// <summary>
        /// Specifies a <see cref="ComponentDataFromEntity{T}"/> to be written to in a shared-write context.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IComponentData"/> in the CDFE.</typeparam>
        /// <returns>A reference to itself to continue chaining configuration methods.</returns>
        public IJobConfig RequireCDFEForWrite<T>()
            where T : struct, IComponentData;

        /// <summary>
        /// Specifies a <see cref="BufferFromEntity{T}"/> to be read from in a shared-read context.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IBufferElementData"/> in the DBFE.</typeparam>
        /// <returns>A reference to itself to continue chaining configuration methods.</returns>
        public IJobConfig RequireDBFEForRead<T>()
            where T : struct, IBufferElementData;

        /// <summary>
        /// Specifies a <see cref="BufferFromEntity{T}"/> to be written to in an exclusive-write context.
        /// </summary>
        /// <typeparam name="T">The type of <see cref="IBufferElementData"/> in the DBFE.</typeparam>
        /// <returns>A reference to itself to continue chaining configuration methods.</returns>
        public IJobConfig RequireDBFEForExclusiveWrite<T>()
            where T : struct, IBufferElementData;

        /// <summary>
        /// Specifies a <see cref="EntityCommandBuffer"/> to be populated.
        /// </summary>
        /// <param name="ecbSystem">
        /// The <see cref="EntityCommandBufferSystem"/> to create the <see cref="EntityCommandBuffer"/> from.
        /// </param>
        /// <returns>A reference to itself to continue chaining configuration methods.</returns>
        IJobConfig RequireECB(EntityCommandBufferSystem ecbSystem);

        /// <summary>
        /// Specifies a delegate to call to add additional requirements.
        /// This allows requirements to be defined by a static method on the job rather than at the configuration call
        /// site.
        /// </summary>
        /// <param name="taskDriver">The task driver instance that the job is being configured on. (usually this)</param>
        /// <param name="configureRequirements">The delegate to call to configure requirements.</param>
        /// <typeparam name="T">The type of the task driver instance.</typeparam>
        /// <returns>A reference to itself to continue chaining configuration methods.</returns>
        IJobConfig AddRequirementsFrom<T>(T taskDriver, ConfigureJobRequirementsDelegate<T> configureRequirements)
            where T : AbstractTaskDriver;
    }
}