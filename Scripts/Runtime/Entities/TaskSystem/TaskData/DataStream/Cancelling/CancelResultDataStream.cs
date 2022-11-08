namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelResultDataStream<TInstance> : AbstractArrayDataStream<EntityProxyInstanceWrapper<TInstance>>,
                                                       ICancelResultDataStream<TInstance>,
                                                       IUntypedCancelResultDataStream
        where TInstance : unmanaged, IEntityProxyInstance
    {
        public CancelResultDataStream(AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem) : base(taskDriver, taskSystem)
        {
        }
    }
}
