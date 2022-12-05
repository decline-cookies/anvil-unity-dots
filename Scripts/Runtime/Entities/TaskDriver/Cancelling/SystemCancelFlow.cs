using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class SystemCancelFlow : AbstractCancelFlow
    {

        public SystemCancelFlow(AbstractTaskSet owningTaskSet, TaskDriverCancelFlow parent) : base(owningTaskSet, parent)
        {
        }

        internal void BuildRelationshipData(List<CancelRequestDataStream> cancelRequests,
                                            List<byte> contexts)
        {
            //Add ourself
            cancelRequests.Add(OwningTaskSet.CancelRequestDataStream);
            //Add the previous context which will represent the TaskDriver that writes to us
            contexts.Add(Parent.TaskDriverContext);
        }
    }
}
