using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class SystemCancelFlow : AbstractCancelFlow
    {
        private readonly AbstractTaskSystem m_TaskSystem;

        public SystemCancelFlow(AbstractTaskSystem taskSystem, TaskDriverCancelFlow parent) : base(taskSystem.CancelData, parent)
        {
            m_TaskSystem = taskSystem;
        }

        internal void BuildRelationshipData(List<AbstractCancelFlow> cancelFlows,
                                            List<CancelRequestDataStream> cancelRequests,
                                            List<byte> contexts)
        {
            cancelFlows.Add(this);
            //Add ourself
            cancelRequests.Add(CancelData.RequestDataStream);
            //Add the previous context which will represent the TaskDriver that writes to us
            contexts.Add(Parent.TaskDriverContext);
        }
    }
}
