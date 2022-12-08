using System;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractArrayDataStream<T> : AbstractDataStream<T>
        where T : unmanaged, IEquatable<T>
    {
        public sealed override uint LiveID
        {
            get => m_LiveArrayData.ID;
        }

        private readonly LiveArrayData<T> m_LiveArrayData;

        protected AbstractArrayDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            m_LiveArrayData = DataSource.CreateLiveArrayData();
        }
    }
}
