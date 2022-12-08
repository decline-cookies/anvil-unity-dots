using System;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractArrayDataStream<T> : AbstractDataStream<T>
        where T : unmanaged, IEquatable<T>
    {
        private readonly LiveArrayData<T> m_LiveArrayData;

        protected AbstractArrayDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            m_LiveArrayData = DataSource.CreateLiveArrayData();
        }
    }
}
