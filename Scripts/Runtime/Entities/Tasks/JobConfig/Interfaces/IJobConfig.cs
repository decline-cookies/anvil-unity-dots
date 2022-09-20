using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IJobConfig
    {
        //TODO: Docs
        public delegate JobHandle ScheduleJobDelegate(JobHandle jobHandle, JobData jobData, IScheduleInfo scheduleInfo);

        public IJobConfig RequireDataStreamForWrite(AbstractProxyDataStream dataStream);
        public IJobConfig RequireDataStreamForRead(AbstractProxyDataStream dataStream);

        public IJobConfig RequireResolveChannel<TResolveChannel>(TResolveChannel resolveChannel)
            where TResolveChannel : Enum;

        public IJobConfig RequireNativeArrayForWrite<T>(NativeArray<T> array)
            where T : unmanaged;

        public IJobConfig RequireNativeArrayForRead<T>(NativeArray<T> array)
            where T : unmanaged;

        public IJobConfig RequireEntityNativeArrayFromQueryForRead(EntityQuery entityQuery);
    }
}
