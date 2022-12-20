// using System;
// using Unity.Entities;
//
// namespace Anvil.Unity.DOTS.Entities.Tasks
// {
//     internal class AbstractLookupDataStream<T> : AbstractDataStream<T>
//         where T : unmanaged, IEquatable<T>
//     {
//         public sealed override uint ActiveID
//         {
//             get => m_ActiveLookupData.ID;
//         }
//         
//         private readonly ActiveLookupData<T> m_ActiveLookupData;
//         public AbstractLookupDataStream(ITaskSetOwner taskSetOwner) : base(taskSetOwner)
//         {
//             m_ActiveLookupData = DataSource.CreateActiveLookupData();
//         }
//     }
// }
