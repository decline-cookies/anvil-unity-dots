namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DataStream<TInstance> : AbstractArrayDataStream<EntityProxyInstanceWrapper<TInstance>>,
                                           IDriverDataStream<TInstance>,
                                           ISystemDataStream<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        public CancelBehaviour CancelBehaviour { get; }
        
        public DataStream(ITaskSetOwner taskSetOwner, CancelBehaviour cancelBehaviour) : base(taskSetOwner)
        {
            CancelBehaviour = cancelBehaviour;
        }
    }
}
