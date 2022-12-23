using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal interface ITaskSetOwner
    {
        public TaskSet TaskSet { get; }
        public uint ID { get; }
        public World World { get; }
        public AbstractTaskDriverSystem TaskDriverSystem { get; }
        
        public List<AbstractTaskDriver> SubTaskDrivers { get; }
        
        public bool HasCancellableData { get; }

        public void AddResolvableDataStreamsTo(Type type, List<AbstractDataStream> dataStreams);
    }
}
