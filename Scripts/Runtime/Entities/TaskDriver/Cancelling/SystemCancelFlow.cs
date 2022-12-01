using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class SystemCancelFlow : AbstractCancelFlow
    {
        private readonly AbstractTaskDriverSystem m_TaskSystem;

        public SystemCancelFlow(AbstractTaskDriverSystem taskSystem, TaskDriverCancelFlow parent) : base(taskSystem.TaskData, parent)
        {
            m_TaskSystem = taskSystem;
        }

        internal void BuildRelationshipData(List<CancelRequestDataStream> cancelRequests,
                                            List<byte> contexts)
        {
            //Add ourself
            cancelRequests.Add(TaskData.CancelRequestDataStream);
            //Add the previous context which will represent the TaskDriver that writes to us
            contexts.Add(Parent.TaskDriverContext);
        }
    }
}
