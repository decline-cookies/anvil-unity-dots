namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelCompleteDataStream : AbstractArrayDataStream<EntityProxyInstanceID>
    {
        public static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<EntityProxyInstanceID>();

        public CancelCompleteDataStream(AbstractWorkload owningWorkload) : base(owningWorkload)
        {
        }

        //*************************************************************************************************************
        // SERIALIZATION
        //*************************************************************************************************************

        //TODO: #83 - Add support for Serialization. Hopefully from the outside or via extension methods instead of functions
        //here but keeping the TODO for future reminder.

        //*************************************************************************************************************
        // JOBS STRUCTS
        //*************************************************************************************************************

        internal CancelCompleteReader CreateCancelCompleteReader()
        {
            return new CancelCompleteReader(Live.AsDeferredJobArray());
        }
    }
}
