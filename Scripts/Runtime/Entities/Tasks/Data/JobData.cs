using Anvil.Unity.DOTS.Data;
using System;
using Unity.Core;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public class JobData
    {
        public AbstractTaskDriverSystem System
        {
            get;
        }

        public World World
        {
            get;
        }

        public ref readonly TimeData Time
        {
            get => ref World.Time;
        }

        public IScheduleWrapper ScheduleWrapper
        {
            get;
            internal set;
        }

        internal JobData(AbstractTaskDriverSystem system)
        {
            System = system;
            World = System.World;
        }

        public VDJobUpdater<TKey, TInstance> GetUpdater<TKey, TInstance>()
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            //TODO: Exceptions
            VirtualData<TKey, TInstance> typedData = (VirtualData<TKey, TInstance>)m_ReferencedData[typeof(VirtualData<TKey, TInstance>)].Data;
            return typedData.CreateVDJobUpdater();
        }

        public VDJobReader<TInstance> GetReader<TKey, TInstance>()
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            //TODO: Exceptions
            VirtualData<TKey, TInstance> typedData = (VirtualData<TKey, TInstance>)m_ReferencedData[typeof(VirtualData<TKey, TInstance>)].Data;
            return typedData.CreateVDJobReader();
        }

        public VDJobWriter<TInstance> GetWriter<TKey, TInstance>()
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            //TODO: Exceptions
            VirtualData<TKey, TInstance> typedData = (VirtualData<TKey, TInstance>)m_ReferencedData[typeof(VirtualData<TKey, TInstance>)].Data;
            return typedData.CreateVDJobWriter();
        }


        public VDJobResultsDestination<TInstance> GetResultsDestination<TKey, TInstance>()
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            //TODO: Exceptions
            VirtualData<TKey, TInstance> typedData = (VirtualData<TKey, TInstance>)m_ReferencedData[typeof(VirtualData<TKey, TInstance>)].Data;
            return typedData.CreateVDJobResultsDestination();
        }
    }
}
