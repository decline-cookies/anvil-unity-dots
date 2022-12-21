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

        public DataStream(AbstractTaskDriver taskDriver, DataStream<TInstance> systemDataStream) : base(taskDriver, systemDataStream)
        {
            
        }


        public DataStreamPendingWriter<TInstance> CreateDataStreamPendingWriter()
        {
            return new DataStreamPendingWriter<TInstance>(DataSource.PendingWriter, TaskSetOwner.ID, ActiveID);
        }

        public DataStreamActiveReader<TInstance> CreateDataStreamActiveReader()
        {
            return new DataStreamActiveReader<TInstance>(DeferredJobArray);
        }

        public DataStreamUpdater<TInstance> CreateDataStreamUpdater(ResolveTargetTypeLookup resolveTargetTypeLookup)
        {
            return new DataStreamUpdater<TInstance>(DataSource.PendingWriter,
                                                    DeferredJobArray,
                                                    resolveTargetTypeLookup);
        }

    }
}
