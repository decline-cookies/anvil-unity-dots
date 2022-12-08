using System;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractDataStream<T> : AbstractDataStream
        where T : unmanaged, IEquatable<T>
    {
        public abstract uint LiveID { get; }
        protected DataSource<T> DataSource { get; }

        protected AbstractDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            DataSourceSystem dataSourceSystem = taskSetOwner.World.GetOrCreateSystem<DataSourceSystem>();
            DataSource = dataSourceSystem.GetOrCreateDataSource<T>();
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
