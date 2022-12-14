using System;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class AbstractLookupDataStream<T> : AbstractDataStream<T>
        where T : unmanaged, IEquatable<T>
    {
        public sealed override uint ActiveID
        {
            get => m_LiveLookupData.ID;
        }
        
        private readonly LiveLookupData<T> m_LiveLookupData;
        public AbstractLookupDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
        {
            m_LiveLookupData = DataSource.CreateActiveLookupData();
        }
    }
}
