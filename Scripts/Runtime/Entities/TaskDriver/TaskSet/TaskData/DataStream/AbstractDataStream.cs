using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractDataStream<TInstance> : AbstractDataStream
        where TInstance : unmanaged, IEntityProxyInstance
    {
        protected DataSource<TInstance> DataSource { get; }

        protected AbstractDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            TaskDriverManagementSystem taskDriverManagementSystem = taskSetOwner.World.GetOrCreateSystem<TaskDriverManagementSystem>();
            DataSource = taskDriverManagementSystem.GetOrCreateDataSource<TInstance>();
        }

        protected AbstractDataStream(ITaskSetOwner taskSetOwner, DataStream<TInstance> systemDataStream) : base(taskSetOwner)
        {
            DataSource = systemDataStream.DataSource;
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

        public abstract uint GetActiveID();
    }
}
