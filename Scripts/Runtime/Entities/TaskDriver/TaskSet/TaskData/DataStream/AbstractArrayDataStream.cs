using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractArrayDataStream<T> : AbstractDataStream<T>
        where T : unmanaged, IEquatable<T>
    {
        private readonly ActiveArrayData<T> m_ActiveArrayData;
        
        public sealed override uint ActiveID
        {
            get => m_ActiveArrayData.ID;
        }

        public DeferredNativeArrayScheduleInfo ScheduleInfo { get; }

        public NativeArray<T> DeferredJobArray
        {
            get => m_ActiveArrayData.DeferredJobArray;
        }

        protected AbstractArrayDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            m_ActiveArrayData = DataSource.CreateActiveArrayData();
            ScheduleInfo = m_ActiveArrayData.ScheduleInfo;
        }

        public JobHandle AcquireActiveAsync(AccessType accessType)
        {
            return m_ActiveArrayData.AcquireAsync(accessType);
        }

        public void ReleaseActiveAsync(JobHandle dependsOn)
        {
            m_ActiveArrayData.ReleaseAsync(dependsOn);
        }
    }
}
