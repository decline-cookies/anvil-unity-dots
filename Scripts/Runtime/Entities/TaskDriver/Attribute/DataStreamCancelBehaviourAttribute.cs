using System;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public class DataStreamCancelBehaviourAttribute : Attribute
    {
        public CancelBehaviour CancelBehaviour { get; }

        public DataStreamCancelBehaviourAttribute(CancelBehaviour cancelBehaviour)
        {
            CancelBehaviour = cancelBehaviour;
        }
    }
}
