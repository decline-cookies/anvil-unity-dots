namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelResultDataStream<TInstance> : AbstractArrayDataStream<EntityProxyInstanceWrapper<TInstance>>,
                                                       ICancelResultDataStream<TInstance>,
                                                       ICommonCancelResultDataStream<TInstance>,
                                                       IUntypedCancelResultDataStream
        where TInstance : unmanaged, IEntityProxyInstance
    {
        public CancelResultDataStream(AbstractTaskSet owningTaskSet) : base(owningTaskSet)
        {
        }

        //*************************************************************************************************************
        // SERIALIZATION
        //*************************************************************************************************************

        //TODO: #83 - Add support for Serialization. Hopefully from the outside or via extension methods instead of functions
        //here but keeping the TODO for future reminder.
    }
}
