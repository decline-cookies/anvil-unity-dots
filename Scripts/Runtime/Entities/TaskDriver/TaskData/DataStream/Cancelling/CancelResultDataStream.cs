namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelResultDataStream<TInstance> : AbstractArrayDataStream<EntityProxyInstanceWrapper<TInstance>>,
                                                       IDriverCancelResultDataStream<TInstance>,
                                                       ISystemCancelResultDataStream<TInstance>,
                                                       IUntypedCancelResultDataStream
        where TInstance : unmanaged, IEntityProxyInstance
    {
        public CancelResultDataStream(AbstractWorkload owningWorkload) : base(owningWorkload)
        {
        }

        //*************************************************************************************************************
        // SERIALIZATION
        //*************************************************************************************************************

        //TODO: #83 - Add support for Serialization. Hopefully from the outside or via extension methods instead of functions
        //here but keeping the TODO for future reminder.
    }
}
