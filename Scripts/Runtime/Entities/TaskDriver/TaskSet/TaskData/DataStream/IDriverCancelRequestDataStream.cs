using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// An <see cref="IAbstractCancelRequestDataStream"/> that is owned by a <see cref="AbstractTaskDriver"/>.
    /// It represents a cancel request for an <see cref="Entity"/>
    /// </summary>
    public interface IDriverCancelRequestDataStream : IAbstractCancelRequestDataStream
    {
        
    }
}
