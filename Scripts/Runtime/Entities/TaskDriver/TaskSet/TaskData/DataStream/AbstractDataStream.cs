using System;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractDataStream<T> : AbstractDataStream
        where T : unmanaged, IEquatable<T>
    {
        protected DataSource<T> DataSource { get; }

        protected AbstractDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            DataSourceSystem dataSourceSystem = taskSetOwner.World.GetOrCreateSystem<DataSourceSystem>();
            DataSource = dataSourceSystem.GetOrCreateDataSource<T>();
        }
    }

    internal abstract class AbstractDataStream
    {
        protected ITaskSetOwner TaskSetOwner { get; }

        protected AbstractDataStream(ITaskSetOwner taskSetOwner)
        {
            TaskSetOwner = taskSetOwner;
        }
    }
}
