using System;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Represents an instance of data that is keyed off an associated <see cref="Entity"/>
    /// for use in <see cref="IEntityPersistentData{T}"/> type.
    /// </summary>
    public interface IEntityPersistentDataInstance : IDisposable { }
}
