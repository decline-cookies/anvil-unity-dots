using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractDataStream<T> : AbstractDataStream
        where T : unmanaged, IEquatable<T>
    {
        public abstract uint ActiveID { get; }
        protected DataSource<T> DataSource { get; }

        protected AbstractDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            DataSourceSystem dataSourceSystem = taskSetOwner.World.GetOrCreateSystem<DataSourceSystem>();
            DataSource = dataSourceSystem.GetOrCreateDataSource<T>();
        }

        public JobHandle AcquirePendingAsync(AccessType accessType)
        {
            return DataSource.AcquirePendingAsync(accessType);
        }

        public void ReleasePendingAsync(JobHandle dependsOn)
        {
            DataSource.ReleasePendingAsync(dependsOn);
        }
    }

    internal abstract class AbstractDataStream
    {
        internal ITaskSetOwner TaskSetOwner { get; }

        protected AbstractDataStream(ITaskSetOwner taskSetOwner)
        {
            TaskSetOwner = taskSetOwner;
        }
    }
}
