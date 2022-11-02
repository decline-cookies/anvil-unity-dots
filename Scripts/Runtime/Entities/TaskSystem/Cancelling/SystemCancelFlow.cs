using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class SystemCancelFlow : AbstractCancelFlow
    {
        private readonly AbstractTaskSystem m_TaskSystem;

        public SystemCancelFlow(AbstractTaskSystem taskSystem)
        {
            m_TaskSystem = taskSystem;
        }

        internal override void BuildRelationshipData(AbstractCancelFlow parentCancelFlow,
                                                     List<CancelRequestDataStream> cancelRequests,
                                                     List<byte> contexts)
        {
            //Assign our parent
            ParentCancelFlow = parentCancelFlow;
            //Add ourself
            cancelRequests.Add(RequestDataStream);
            //Add the previous context which will represent the TaskDriver that writes to us
            contexts.Add(contexts[^1]);
        }
    }
}
