using System;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal interface ITaskSetOwner : IDataOwner
    {
        public TaskSet TaskSet { get; }
        
        public AbstractTaskDriverSystem TaskDriverSystem { get; }

        public List<AbstractTaskDriver> SubTaskDrivers { get; }

        public bool HasCancellableData { get; }

        public void AddResolvableDataStreamsTo(Type type, List<AbstractDataStream> dataStreams);
    }
}
