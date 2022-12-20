using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractDataStream<TInstance> : AbstractDataStream
        where TInstance : unmanaged, IEntityProxyInstance
    {
        public abstract uint ActiveID { get; }
        protected DataSource<TInstance> DataSource { get; }

        protected AbstractDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            TaskDriverManagementSystem taskDriverManagementSystem = taskSetOwner.World.GetOrCreateSystem<TaskDriverManagementSystem>();
            DataSource = taskDriverManagementSystem.GetOrCreateDataSource<TInstance>();
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
