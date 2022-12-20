namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DataStream<TInstance> : AbstractArrayDataStream<TInstance>,
                                           IDriverDataStream<TInstance>,
                                           ISystemDataStream<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        public static readonly int MAX_ELEMENTS_PER_CHUNK = ChunkUtil.MaxElementsPerChunk<EntityProxyInstanceWrapper<TInstance>>();
        
        public CancelBehaviour CancelBehaviour { get; }
        
        public DataStream(ITaskSetOwner taskSetOwner, CancelBehaviour cancelBehaviour) : base(taskSetOwner)
        {
            CancelBehaviour = cancelBehaviour;
        }


        public DataStreamPendingWriter<TInstance> CreateDataStreamPendingWriter()
        {
            //When writing explicitly to a driver data stream, it's going into a global pending bucket. We need to write the ID of the Active bucket to write to when it's consolidated.
            //When writing explicitly to a system data stream, it's going into a global pending bucket but that will be consolidated into one giant active bucket. 
            //We need to write the ID of the TaskDriver that we got access to this SystemDataStream from and write that. When we resolve to a type, we'll know which 
            //TaskDriver to write to.
            uint context = (TaskSetOwner == TaskSetOwner.TaskDriverSystem)
                ? TaskSetOwner.ID
                : ActiveID;
            return new DataStreamPendingWriter<TInstance>(DataSource.PendingWriter, context);
        }

        public DataStreamActiveReader<TInstance> CreateDataStreamActiveReader()
        {
            return new DataStreamActiveReader<TInstance>(DeferredJobArray);
        }

        public DataStreamUpdater<TInstance> CreateDataStreamUpdater(DataStreamTargetResolver targetResolver)
        {
            return new DataStreamUpdater<TInstance>(DataSource.PendingWriter,
                                                    DeferredJobArray,
                                                    targetResolver);
        }

    }
}
