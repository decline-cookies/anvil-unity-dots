using System;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class AbstractLookupDataStream<T> : AbstractDataStream<T>
        where T : unmanaged, IEquatable<T>
    {
        private readonly LiveLookupData<T> m_LiveLookupData;
        public AbstractLookupDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            m_LiveLookupData = DataSource.CreateLiveLookupData();
        }
    }
}
