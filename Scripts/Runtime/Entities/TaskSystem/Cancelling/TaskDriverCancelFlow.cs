using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class TaskDriverCancelFlow : AbstractCancelFlow
    {
        private readonly AbstractTaskDriver m_TaskDriver;

        public TaskDriverCancelFlow(AbstractTaskDriver taskDriver)
        {
            m_TaskDriver = taskDriver;
        }

        internal override void BuildRelationshipData(AbstractCancelFlow parentCancelFlow,
                                                     List<CancelRequestDataStream> cancelRequests,
                                                     List<byte> contexts)
        {
            //Assign our parent
            ParentCancelFlow = parentCancelFlow;
            //Add ourself
            cancelRequests.Add(RequestDataStream);
            //Add our TaskDriver's context
            contexts.Add(m_TaskDriver.Context);
            //Add our governing system
            m_TaskDriver.TaskSystem.CancelFlow.BuildRelationshipData(parentCancelFlow, cancelRequests, contexts);
            //For all subtask drivers, recursively add
            foreach (AbstractTaskDriver taskDriver in m_TaskDriver.SubTaskDrivers)
            {
                taskDriver.CancelFlow.BuildRelationshipData(parentCancelFlow, cancelRequests, contexts);
            }
        }
    }
}
