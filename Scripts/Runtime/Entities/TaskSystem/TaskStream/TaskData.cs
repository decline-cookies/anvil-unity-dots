using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class TaskData : AbstractAnvilBase
    {
        public readonly List<IInternalDataStream> DataStreams;
        public readonly List<IInternalCancellableDataStream> CancellableDataStreams;
        public readonly List<IInternalCancelResultDataStream> CancelResultDataStreams;
        public readonly CancelRequestDataStream CancelRequestDataStream;
        public readonly CancelCompleteDataStream CancelCompleteDataStream;
        public readonly AbstractTaskDriver TaskDriver;
        public readonly AbstractTaskSystem TaskSystem;
        public readonly byte Context;

        private readonly AccessControlledValue<UnsafeParallelHashMap<EntityProxyInstanceID, bool>> m_CancelProgressLookup;
        private readonly List<IInternalAbstractDataStream> m_AllPublicDataStreams;
        

        public TaskData(AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem)
        {
            TaskDriver = taskDriver;
            TaskSystem = taskSystem;

            m_AllPublicDataStreams = new List<IInternalAbstractDataStream>();

            DataStreams = new List<IInternalDataStream>();
            CancellableDataStreams = new List<IInternalCancellableDataStream>();
            CancelResultDataStreams = new List<IInternalCancelResultDataStream>();
            m_CancelProgressLookup = new AccessControlledValue<UnsafeParallelHashMap<EntityProxyInstanceID, bool>>(new UnsafeParallelHashMap<EntityProxyInstanceID, bool>(ChunkUtil.MaxElementsPerChunk<EntityProxyInstanceID>(),
                                                                                                                                                                          Allocator.Persistent));
            CancelRequestDataStream = new CancelRequestDataStream(m_CancelProgressLookup, TaskDriver, TaskSystem);
            CancelCompleteDataStream = new CancelCompleteDataStream(TaskDriver, TaskSystem);
            
            DataStreamFactory.CreateDataStreams(this);
        }

        protected override void DisposeSelf()
        {
            //TODO: Should this get baked into AccessControlledValue's Dispose method?
            m_CancelProgressLookup.Acquire(AccessType.Disposal);
            m_CancelProgressLookup.Dispose();
            
            CancelRequestDataStream.Dispose();
            CancelCompleteDataStream.Dispose();
            
            m_AllPublicDataStreams.DisposeAllAndTryClear();
            
            DataStreams.Clear();
            CancellableDataStreams.Clear();
            CancelResultDataStreams.Clear();
            
            
            base.DisposeSelf();
        }

        public void RegisterDataStream<TInstance>(DataStream<TInstance> dataStream)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            DataStreams.Add(dataStream);
            m_AllPublicDataStreams.Add(dataStream);
        }
        
        public void RegisterDataStream<TInstance>(CancellableDataStream<TInstance> dataStream)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            CancellableDataStreams.Add(dataStream);
            m_AllPublicDataStreams.Add(dataStream);
        }
        
        public void RegisterDataStream<TInstance>(CancelResultDataStream<TInstance> dataStream)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            CancelResultDataStreams.Add(dataStream);
            m_AllPublicDataStreams.Add(dataStream);
        }
    }
}
