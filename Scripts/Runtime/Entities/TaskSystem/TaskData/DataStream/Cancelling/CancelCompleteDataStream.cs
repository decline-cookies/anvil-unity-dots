namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelCompleteDataStream : AbstractArrayDataStream<EntityProxyInstanceID>
    {
        public static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<EntityProxyInstanceID>();
        
        internal CancelCompleteDataStream(AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem) : base(taskDriver, taskSystem)
        {
        }
        
        //*************************************************************************************************************
        // JOBS STRUCTS
        //*************************************************************************************************************
        
        internal CancelCompleteReader CreateCancelCompleteReader()
        {
            return new CancelCompleteReader(Live.AsDeferredJobArray());
        }
    }
}
