using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class NoOpJobConfig : IResolvableJobConfigRequirements
    {
        public bool IsEnabled
        {
            get;
            set;
        }

        public IJobConfig RunOnce()
        {
            return this;
        }

        public IJobConfig RequireDataStreamForWrite<TInstance>(IAbstractDataStream<TInstance> dataStream)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            return this;
        }

        public IJobConfig RequireDataStreamForRead<TInstance>(IAbstractDataStream<TInstance> dataStream)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            return this;
        }

        public IJobConfig RequireGenericDataForRead<TData>(AccessControlledValue<TData> data)
            where TData : struct
        {
            return this;
        }

        public IJobConfig RequireGenericDataForWrite<TData>(AccessControlledValue<TData> data)
            where TData : struct
        {
            return this;
        }

        public IJobConfig RequireGenericDataForExclusiveWrite<TData>(AccessControlledValue<TData> data)
            where TData : struct
        {
            return this;
        }

        public IJobConfig RequireEntityNativeArrayFromQueryForRead(EntityQuery entityQuery)
        {
            return this;
        }

        public IJobConfig RequireIComponentDataNativeArrayFromQueryForRead<T>(EntityQuery entityQuery)
            where T : struct, IComponentData
        {
            return this;
        }

        public IJobConfig RequireCDFEForRead<T>()
            where T : struct, IComponentData
        {
            return this;
        }

        public IJobConfig RequireCDFEForWrite<T>()
            where T : struct, IComponentData
        {
            return this;
        }

        public IJobConfig RequireDBFEForRead<T>()
            where T : struct, IBufferElementData
        {
            return this;
        }

        public IJobConfig RequireDBFEForExclusiveWrite<T>()
            where T : struct, IBufferElementData
        {
            return this;
        }

        public IResolvableJobConfigRequirements RequireResolveTarget<TResolveTargetType>()
            where TResolveTargetType : unmanaged, IEntityProxyInstance
        {
            return this;
        }

        public IJobConfig RequestCancelFor(AbstractTaskDriver taskDriver)
        {
            return this;
        }

        public IJobConfig RequireThreadPersistentDataForWrite<TData>()
            where TData : unmanaged, IThreadPersistentDataInstance
        {
            return this;
        }

        public IJobConfig RequireThreadPersistentDataForRead<TData>()
            where TData : unmanaged, IThreadPersistentDataInstance
        {
            return this;
        }

        public IJobConfig RequireEntityPersistentDataForWrite<TData>(IEntityPersistentData<TData> entityPersistentData)
            where TData : unmanaged, IEntityPersistentDataInstance
        {
            return this;
        }

        public IJobConfig RequireEntityPersistentDataForRead<TData>(IEntityPersistentData<TData> entityPersistentData)
            where TData : unmanaged, IEntityPersistentDataInstance
        {
            return this;
        }

        public IJobConfig AddRequirementsFrom<T>(T taskDriver, Func<T, IJobConfig, IJobConfig> configureRequirements)
            where T : AbstractTaskDriver
        {
            return configureRequirements(taskDriver, this);
        }
    }
}
