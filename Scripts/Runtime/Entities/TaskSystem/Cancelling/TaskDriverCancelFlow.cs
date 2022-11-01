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

        protected override void AddRequestDataTo(List<CancelRequestDataStream> cancelRequests, List<byte> contexts)
        {
            //Add ourself
            cancelRequests.Add(RequestDataStream);
            //Add our TaskDriver's context
            contexts.Add(m_TaskDriver.Context);
            //Add our governing system
            m_TaskDriver.CancelFlow.AddRequestDataTo(cancelRequests, contexts);
            //For all subtask drivers, recursively add
            foreach (AbstractTaskDriver taskDriver in m_TaskDriver.SubTaskDrivers)
            {
                taskDriver.CancelFlow.AddRequestDataTo(cancelRequests, contexts);
            }
        }
    }
}
