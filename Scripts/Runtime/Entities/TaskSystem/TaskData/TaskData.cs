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
        public readonly List<AbstractDataStream> DataStreams;
        public readonly List<AbstractDataStream> CancellableDataStreams;
        public readonly List<AbstractDataStream> CancelResultDataStreams;
        public readonly CancelRequestDataStream CancelRequestDataStream;
        public readonly CancelCompleteDataStream CancelCompleteDataStream;
        public readonly AbstractTaskDriver TaskDriver;
        public readonly AbstractTaskSystem TaskSystem;
        public readonly AccessControlledValue<UnsafeParallelHashMap<EntityProxyInstanceID, bool>> CancelProgressLookup;
        
        public readonly List<AbstractDataStream> AllPublicDataStreams;
        

        public TaskData(AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem)
        {
            TaskDriver = taskDriver;
            TaskSystem = taskSystem;

            AllPublicDataStreams = new List<AbstractDataStream>();

            DataStreams = new List<AbstractDataStream>();
            CancellableDataStreams = new List<AbstractDataStream>();
            CancelResultDataStreams = new List<AbstractDataStream>();
            CancelProgressLookup = new AccessControlledValue<UnsafeParallelHashMap<EntityProxyInstanceID, bool>>(new UnsafeParallelHashMap<EntityProxyInstanceID, bool>(ChunkUtil.MaxElementsPerChunk<EntityProxyInstanceID>(),
                                                                                                                                                                          Allocator.Persistent));
            CancelRequestDataStream = new CancelRequestDataStream(CancelProgressLookup, TaskDriver, TaskSystem);
            CancelCompleteDataStream = new CancelCompleteDataStream(TaskDriver, TaskSystem);
            
            DataStreamFactory.CreateDataStreams(this);
        }

        protected override void DisposeSelf()
        {
            //TODO: #104 - Should this get baked into AccessControlledValue's Dispose method?
            CancelProgressLookup.Acquire(AccessType.Disposal);
            CancelProgressLookup.Dispose();
            
            CancelRequestDataStream.Dispose();
            CancelCompleteDataStream.Dispose();
            
            AllPublicDataStreams.DisposeAllAndTryClear();
            
            DataStreams.Clear();
            CancellableDataStreams.Clear();
            CancelResultDataStreams.Clear();
            
            
            base.DisposeSelf();
        }

        public void RegisterDataStream<TInstance>(DataStream<TInstance> dataStream)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            DataStreams.Add(dataStream);
            AllPublicDataStreams.Add(dataStream);
        }
        
        public void RegisterDataStream<TInstance>(CancellableDataStream<TInstance> dataStream)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            CancellableDataStreams.Add(dataStream);
            AllPublicDataStreams.Add(dataStream);
        }
        
        public void RegisterDataStream<TInstance>(CancelResultDataStream<TInstance> dataStream)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            CancelResultDataStreams.Add(dataStream);
            AllPublicDataStreams.Add(dataStream);
        }
    }
}
